/*
 * Copyright (C) 2010 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Billing
{
    /// <summary>
    /// This class holds global constants that are used throughout the application
    /// to support in-app billing.
    /// </summary>
    public class Consts
    {
        // The response codes for a request, defined by Android Market.
        public enum ResponseCode
        {
            RESULT_OK,
            RESULT_USER_CANCELED,
            RESULT_SERVICE_UNAVAILABLE,
            RESULT_BILLING_UNAVAILABLE,
            RESULT_ITEM_UNAVAILABLE,
            RESULT_DEVELOPER_ERROR,
            RESULT_ERROR

            // Converts from an ordinal value to the ResponseCode
            //JAVA TO C# CONVERTER TODO TASK: Enums cannot contain methods in .NET:
            //			public static ResponseCode valueOf(int index)
            //		{
            //			ResponseCode[] values = ResponseCode.values();
            //			if (index < 0 || index >= values.length)
            //			{
            //				return RESULT_ERROR;
            //			}
            //			return values[index];
            //		}
        }

        // The possible states of an in-app purchase, as defined by Android Market.
        public enum PurchaseState
        {
            // Responses to requestPurchase or restoreTransactions.
            PURCHASED, // User was charged for the order.
            CANCELED, // The charge failed on the server.
            REFUNDED // User received a refund for the order.

            // Converts from an ordinal value to the PurchaseState
            //JAVA TO C# CONVERTER TODO TASK: Enums cannot contain methods in .NET:
            //			public static PurchaseState valueOf(int index)
            //		{
            //			PurchaseState[] values = PurchaseState.values();
            //			if (index < 0 || index >= values.length)
            //			{
            //				return CANCELED;
            //			}
            //			return values[index];
            //		}
        }

        /// <summary>
        /// This is the action we use to bind to the MarketBillingService. </summary>
        public const string MARKET_BILLING_SERVICE_ACTION = "com.android.vending.billing.MarketBillingService.BIND";

        // Intent actions that we send from the BillingReceiver to the
        // BillingService.  Defined by this application.
        public const string ACTION_CONFIRM_NOTIFICATION = "com.example.subscriptions.CONFIRM_NOTIFICATION";
        public const string ACTION_GET_PURCHASE_INFORMATION = "com.example.subscriptions.GET_PURCHASE_INFORMATION";
        public const string ACTION_RESTORE_TRANSACTIONS = "com.example.subscriptions.RESTORE_TRANSACTIONS";

        // Intent actions that we receive in the BillingReceiver from Market.
        // These are defined by Market and cannot be changed.
        public const string ACTION_NOTIFY = "com.android.vending.billing.IN_APP_NOTIFY";
        public const string ACTION_RESPONSE_CODE = "com.android.vending.billing.RESPONSE_CODE";
        public const string ACTION_PURCHASE_STATE_CHANGED = "com.android.vending.billing.PURCHASE_STATE_CHANGED";

        // These are the names of the extras that are passed in an intent from
        // Market to this application and cannot be changed.
        public const string NOTIFICATION_ID = "notification_id";
        public const string INAPP_SIGNED_DATA = "inapp_signed_data";
        public const string INAPP_SIGNATURE = "inapp_signature";
        public const string INAPP_REQUEST_ID = "request_id";
        public const string INAPP_RESPONSE_CODE = "response_code";

        // These are the names of the fields in the request bundle.
        public const string BILLING_REQUEST_METHOD = "BILLING_REQUEST";
        public const string BILLING_REQUEST_API_VERSION = "API_VERSION";
        public const string BILLING_REQUEST_PACKAGE_NAME = "PACKAGE_NAME";
        public const string BILLING_REQUEST_ITEM_ID = "ITEM_ID";
        public const string BILLING_REQUEST_ITEM_TYPE = "ITEM_TYPE";
        public const string BILLING_REQUEST_DEVELOPER_PAYLOAD = "DEVELOPER_PAYLOAD";
        public const string BILLING_REQUEST_NOTIFY_IDS = "NOTIFY_IDS";
        public const string BILLING_REQUEST_NONCE = "NONCE";

        public const string BILLING_RESPONSE_RESPONSE_CODE = "RESPONSE_CODE";
        public const string BILLING_RESPONSE_PURCHASE_INTENT = "PURCHASE_INTENT";
        public const string BILLING_RESPONSE_REQUEST_ID = "REQUEST_ID";
        public static long BILLING_RESPONSE_INVALID_REQUEST_ID = -1;

        // These are the types supported in the IAB v2
        public const string ITEM_TYPE_INAPP = "inapp";
        public const string ITEM_TYPE_SUBSCRIPTION = "subs";

        public const bool DEBUG = true;
    }
}