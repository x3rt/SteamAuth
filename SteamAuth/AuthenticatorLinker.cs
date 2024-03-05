﻿namespace SteamAuth
{
    using System;
    using System.Collections.Specialized;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    ///     Handles the linking process for a new mobile authenticator.
    /// </summary>
    public class AuthenticatorLinker
    {
        /// <summary>
        ///     Session data containing an access token for a steam account generated with k_EAuthTokenPlatformType_MobileApp
        /// </summary>
        private readonly SessionData _session;

        /// <summary>
        ///     Set to register a new phone number when linking. If a phone number is not set on the account, this must be set. If
        ///     a phone number is set on the account, this must be null.
        /// </summary>
        public string PhoneNumber = null;

        public string PhoneCountryCode = null;

        /// <summary>
        ///     Randomly-generated device ID. Should only be generated once per linker.
        /// </summary>
        private string DeviceId { get; }

        /// <summary>
        ///     After the initial link step, if successful, this will be the SteamGuard data for the account. PLEASE save this
        ///     somewhere after generating it; it's vital data.
        /// </summary>
        public SteamGuardAccount LinkedAccount { get; private set; }

        /// <summary>
        ///     True if the authenticator has been fully finalized.
        /// </summary>
        public bool Finalized = false;

        /// <summary>
        ///     Set when the confirmation email to set a phone number is set
        /// </summary>
        private bool _confirmationEmailSent;

        /// <summary>
        ///     Email address the confirmation email was sent to when adding a phone number
        /// </summary>
        public string ConfirmationEmailAddress;

        /// <summary>
        ///     Create a new instance of AuthenticatorLinker
        /// </summary>
        /// <param name="sessionData">
        ///     Session data containing an access token for a steam account generated with
        ///     k_EAuthTokenPlatformType_MobileApp
        /// </param>
        public AuthenticatorLinker(SessionData sessionData)
        {
            _session = sessionData;
            DeviceId = GenerateDeviceId();
        }

        /// <summary>
        ///     First step in adding a mobile authenticator to an account
        /// </summary>
        public async Task<LinkResult> AddAuthenticator()
        {
            // This method will be called again once the user confirms their phone number email
            if (_confirmationEmailSent)
            {
                // Check if email was confirmed
                bool isStillWaiting = await _isAccountWaitingForEmailConfirmation();
                if (isStillWaiting)
                {
                    return LinkResult.MustConfirmEmail;
                }

                // Now send the SMS to the phone number
                await _sendPhoneVerificationCode();

                // This takes time so wait a bit
                await Task.Delay(2000);
            }

            // Make request to ITwoFactorService/AddAuthenticator
            NameValueCollection addAuthenticatorBody = new NameValueCollection();
            addAuthenticatorBody.Add("steamid", _session.SteamId.ToString());
            addAuthenticatorBody.Add("authenticator_time", (await TimeAligner.GetSteamTimeAsync()).ToString());
            addAuthenticatorBody.Add("authenticator_type", "1");
            addAuthenticatorBody.Add("device_identifier", DeviceId);
            addAuthenticatorBody.Add("sms_phone_id", "1");
            string addAuthenticatorResponseStr = await SteamWeb.PostRequest(
                "https://api.steampowered.com/ITwoFactorService/AddAuthenticator/v1/?access_token=" + _session.AccessToken,
                null,
                addAuthenticatorBody);

            // Parse response json to object
            AddAuthenticatorResponse addAuthenticatorResponse =
                JsonConvert.DeserializeObject<AddAuthenticatorResponse>(addAuthenticatorResponseStr);

            if (addAuthenticatorResponse == null || addAuthenticatorResponse.Response == null)
            {
                return LinkResult.GeneralFailure;
            }

            // Status 2 means no phone number is on the account
            if (addAuthenticatorResponse.Response.Status == 2)
            {
                if (PhoneNumber == null)
                {
                    return LinkResult.MustProvidePhoneNumber;
                }

                // Add phone number

                // Get country code
                string countryCode = PhoneCountryCode;

                // If given country code is null, use the one from the Steam account
                if (string.IsNullOrEmpty(countryCode))
                {
                    countryCode = await GetUserCountry();
                }

                // Set the phone number
                SetAccountPhoneNumberResponse res = await _setAccountPhoneNumber(PhoneNumber, countryCode);

                // Make sure it's successful then respond that we must confirm via email
                if (res != null && res.Response.ConfirmationEmailAddress != null)
                {
                    ConfirmationEmailAddress = res.Response.ConfirmationEmailAddress;
                    _confirmationEmailSent = true;
                    return LinkResult.MustConfirmEmail;
                }

                // If something else fails, we end up here
                return LinkResult.FailureAddingPhone;
            }

            if (addAuthenticatorResponse.Response.Status == 29)
            {
                return LinkResult.AuthenticatorPresent;
            }

            if (addAuthenticatorResponse.Response.Status != 1)
            {
                return LinkResult.GeneralFailure;
            }

            // Setup this.LinkedAccount
            LinkedAccount = addAuthenticatorResponse.Response;
            LinkedAccount.DeviceId = DeviceId;
            LinkedAccount.Session = _session;

            return LinkResult.AwaitingFinalization;
        }

        public async Task<FinalizeResult> FinalizeAddAuthenticator(string smsCode)
        {
            int tries = 0;
            while (tries <= 10)
            {
                NameValueCollection finalizeAuthenticatorValues = new NameValueCollection();
                finalizeAuthenticatorValues.Add("steamid", _session.SteamId.ToString());
                finalizeAuthenticatorValues.Add("authenticator_code", LinkedAccount.GenerateSteamGuardCode());
                finalizeAuthenticatorValues.Add("authenticator_time", TimeAligner.GetSteamTime().ToString());
                finalizeAuthenticatorValues.Add("activation_code", smsCode);
                finalizeAuthenticatorValues.Add("validate_sms_code", "1");

                string finalizeAuthenticatorResultStr;
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers[HttpRequestHeader.UserAgent] = SteamWeb.MobileAppUserAgent;
                    byte[] finalizeAuthenticatorResult = await wc.UploadValuesTaskAsync(
                        new Uri(
                            "https://api.steampowered.com/ITwoFactorService/FinalizeAddAuthenticator/v1/?access_token=" + _session.AccessToken),
                        "POST",
                        finalizeAuthenticatorValues);
                    finalizeAuthenticatorResultStr = Encoding.UTF8.GetString(finalizeAuthenticatorResult);
                }

                FinalizeAuthenticatorResponse finalizeAuthenticatorResponse =
                    JsonConvert.DeserializeObject<FinalizeAuthenticatorResponse>(finalizeAuthenticatorResultStr);

                if (finalizeAuthenticatorResponse == null || finalizeAuthenticatorResponse.Response == null)
                {
                    return FinalizeResult.GeneralFailure;
                }

                if (finalizeAuthenticatorResponse.Response.Status == 89)
                {
                    return FinalizeResult.BadSmsCode;
                }

                if (finalizeAuthenticatorResponse.Response.Status == 88)
                {
                    if (tries >= 10)
                    {
                        return FinalizeResult.UnableToGenerateCorrectCodes;
                    }
                }

                if (!finalizeAuthenticatorResponse.Response.Success)
                {
                    return FinalizeResult.GeneralFailure;
                }

                if (finalizeAuthenticatorResponse.Response.WantMore)
                {
                    tries++;
                    continue;
                }

                LinkedAccount.FullyEnrolled = true;
                return FinalizeResult.Success;
            }

            return FinalizeResult.GeneralFailure;
        }

        private async Task<string> GetUserCountry()
        {
            NameValueCollection getCountryBody = new NameValueCollection();
            getCountryBody.Add("steamid", _session.SteamId.ToString());
            string getCountryResponseStr = await SteamWeb.PostRequest(
                "https://api.steampowered.com/IUserAccountService/GetUserCountry/v1?access_token=" + _session.AccessToken,
                null,
                getCountryBody);

            // Parse response json to object
            GetUserCountryResponse response = JsonConvert.DeserializeObject<GetUserCountryResponse>(getCountryResponseStr);
            return response.Response.Country;
        }

        private async Task<SetAccountPhoneNumberResponse> _setAccountPhoneNumber(string phoneNumber, string countryCode)
        {
            NameValueCollection setPhoneBody = new NameValueCollection();
            setPhoneBody.Add("phone_number", phoneNumber);
            setPhoneBody.Add("phone_country_code", countryCode);
            string getCountryResponseStr = await SteamWeb.PostRequest(
                "https://api.steampowered.com/IPhoneService/SetAccountPhoneNumber/v1?access_token=" + _session.AccessToken,
                null,
                setPhoneBody);
            return JsonConvert.DeserializeObject<SetAccountPhoneNumberResponse>(getCountryResponseStr);
        }

        private async Task<bool> _isAccountWaitingForEmailConfirmation()
        {
            string waitingForEmailResponse = await SteamWeb.PostRequest(
                "https://api.steampowered.com/IPhoneService/IsAccountWaitingForEmailConfirmation/v1?access_token=" + _session.AccessToken,
                null,
                null);

            // Parse response json to object
            IsAccountWaitingForEmailConfirmationResponse response =
                JsonConvert.DeserializeObject<IsAccountWaitingForEmailConfirmationResponse>(waitingForEmailResponse);
            return response.Response.AwaitingEmailConfirmation;
        }

        private async Task _sendPhoneVerificationCode()
        {
            await SteamWeb.PostRequest(
                "https://api.steampowered.com/IPhoneService/SendPhoneVerificationCode/v1?access_token=" + _session.AccessToken,
                null,
                null);
        }

        public enum LinkResult
        {
            MustProvidePhoneNumber, //No phone number on the account
            MustRemovePhoneNumber, //A phone number is already on the account
            MustConfirmEmail, //User need to click link from confirmation email
            AwaitingFinalization, //Must provide an SMS code
            GeneralFailure, //General failure (really now!)
            AuthenticatorPresent,
            FailureAddingPhone,
        }

        public enum FinalizeResult
        {
            BadSmsCode,
            UnableToGenerateCorrectCodes,
            Success,
            GeneralFailure,
        }

        private class GetUserCountryResponse
        {
            [JsonProperty("response")]
            public GetUserCountryResponseResponse Response { get; set; }
        }

        private class GetUserCountryResponseResponse
        {
            [JsonProperty("country")]
            public string Country { get; set; }
        }

        private class SetAccountPhoneNumberResponse
        {
            [JsonProperty("response")]
            public SetAccountPhoneNumberResponseResponse Response { get; set; }
        }

        private class SetAccountPhoneNumberResponseResponse
        {
            [JsonProperty("confirmation_email_address")]
            public string ConfirmationEmailAddress { get; set; }

            [JsonProperty("phone_number_formatted")]
            public string PhoneNumberFormatted { get; set; }
        }

        private class IsAccountWaitingForEmailConfirmationResponse
        {
            [JsonProperty("response")]
            public IsAccountWaitingForEmailConfirmationResponseResponse Response { get; set; }
        }

        private class IsAccountWaitingForEmailConfirmationResponseResponse
        {
            [JsonProperty("awaiting_email_confirmation")]
            public bool AwaitingEmailConfirmation { get; set; }

            [JsonProperty("seconds_to_wait")]
            public int SecondsToWait { get; set; }
        }

        private class AddAuthenticatorResponse
        {
            [JsonProperty("response")]
            public SteamGuardAccount Response { get; set; }
        }

        private class FinalizeAuthenticatorResponse
        {
            [JsonProperty("response")]
            public FinalizeAuthenticatorInternalResponse Response { get; set; }

            internal class FinalizeAuthenticatorInternalResponse
            {
                [JsonProperty("success")]
                public bool Success { get; set; }

                [JsonProperty("want_more")]
                public bool WantMore { get; set; }

                [JsonProperty("server_time")]
                public long ServerTime { get; set; }

                [JsonProperty("status")]
                public int Status { get; set; }
            }
        }

        public static string GenerateDeviceId()
        {
            return "android:" + Guid.NewGuid();
        }
    }
}