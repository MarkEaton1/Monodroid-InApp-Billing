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

using Android.Content;
using Android.Util;

namespace Billing
{
    /// <summary>
    /// This class implements the broadcast receiver for in-app billing. All asynchronous messages from
    /// Android Market come to this app through this receiver. This class forwards all
    /// messages to the <seealso cref="BillingService"/>, which can start background threads,
    /// if necessary, to process the messages. This class runs on the UI thread and must not do any
    /// network I/O, database updates, or any tasks that might take a long time to complete.
    /// It also must not start a background thread because that may be killed as soon as
    /// <seealso cref="#onReceive(Context, Intent)"/> returns.
    /// 
    /// You should modify and obfuscate this code before using it.
    /// </summary>
    [BroadcastReceiver]
    [Android.App.IntentFilter(new string[]{"com.android.vending.billing.IN_APP_NOTIFY", "com.android.vending.billing.RESPONSE_CODE", "com.android.vending.billing.PURCHASE_STATE_CHANGED"})]
    public class BillingReceiver : BroadcastReceiver
    {
        private const string TAG = "BillingReceiver";

        /// <summary>
        /// This is the entry point for all asynchronous messages sent from Android Market to
        /// the application. This method forwards the messages on to the
        /// <seealso cref="BillingService"/>, which handles the communication back to Android Market.
        /// The <seealso cref="BillingService"/> also reports state changes back to the application through
        /// the <seealso cref="ResponseHandler"/>.
        /// </summary>
        public override void OnReceive(Context context, Intent intent)
        {
            string action = intent.Action;
            if (Consts.ACTION_PURCHASE_STATE_CHANGED.Equals(action))
            {
                string signedData = intent.GetStringExtra(Consts.INAPP_SIGNED_DATA);
                string signature = intent.GetStringExtra(Consts.INAPP_SIGNATURE);
                PurchaseStateChanged(context, signedData, signature);
            }
            else if (Consts.ACTION_NOTIFY.Equals(action))
            {
                string notifyId = intent.GetStringExtra(Consts.NOTIFICATION_ID);
                if (Consts.DEBUG)
                {
                    Log.Info(TAG, "notifyId: " + notifyId);
                }
                Notify(context, notifyId);
            }
            else if (Consts.ACTION_RESPONSE_CODE.Equals(action))
            {
                long requestId = intent.GetLongExtra(Consts.INAPP_REQUEST_ID, -1);
                int responseCodeIndex = intent.GetIntExtra(Consts.INAPP_RESPONSE_CODE, (int)Consts.ResponseCode.RESULT_ERROR);
                CheckResponseCode(context, requestId, responseCodeIndex);
            }
            else
            {
                Log.Warn(TAG, "unexpected action: " + action);
            }
        }

        /// <summary>
        /// This is called when Android Market sends information about a purchase state
        /// change. The signedData parameter is a plaintext JSON string that is
        /// signed by the server with the developer's private key. The signature
        /// for the signed data is passed in the signature parameter. </summary>
        /// <param name="context"> the context </param>
        /// <param name="signedData"> the (unencrypted) JSON string </param>
        /// <param name="signature"> the signature for the signedData </param>
        private void PurchaseStateChanged(Context context, string signedData, string signature)
        {
            Intent intent = new Intent(Consts.ACTION_PURCHASE_STATE_CHANGED);
            intent.SetClass(context, typeof(BillingService));
            intent.PutExtra(Consts.INAPP_SIGNED_DATA, signedData);
            intent.PutExtra(Consts.INAPP_SIGNATURE, signature);
            context.StartService(intent);
        }

        /// <summary>
        /// This is called when Android Market sends a "notify" message  indicating that transaction
        /// information is available. The request includes a nonce (random number used once) that
        /// we generate and Android Market signs and sends back to us with the purchase state and
        /// other transaction details. This BroadcastReceiver cannot bind to the
        /// MarketBillingService directly so it starts the <seealso cref="BillingService"/>, which does the
        /// actual work of sending the message.
        /// </summary>
        /// <param name="context"> the context </param>
        /// <param name="notifyId"> the notification ID </param>
        private void Notify(Context context, string notifyId)
        {
            Intent intent = new Intent(Consts.ACTION_GET_PURCHASE_INFORMATION);
            intent.SetClass(context, typeof(BillingService));
            intent.PutExtra(Consts.NOTIFICATION_ID, notifyId);
            context.StartService(intent);
        }

        /// <summary>
        /// This is called when Android Market sends a server response code. The BillingService can
        /// then report the status of the response if desired.
        /// </summary>
        /// <param name="context"> the context </param>
        /// <param name="requestId"> the request ID that corresponds to a previous request </param>
        /// <param name="responseCodeIndex"> the ResponseCode ordinal value for the request </param>
        private void CheckResponseCode(Context context, long requestId, int responseCodeIndex)
        {
            Intent intent = new Intent(Consts.ACTION_RESPONSE_CODE);
            intent.SetClass(context, typeof(BillingService));
            intent.PutExtra(Consts.INAPP_REQUEST_ID, requestId);
            intent.PutExtra(Consts.INAPP_RESPONSE_CODE, responseCodeIndex);
            context.StartService(intent);
        }
    }
}