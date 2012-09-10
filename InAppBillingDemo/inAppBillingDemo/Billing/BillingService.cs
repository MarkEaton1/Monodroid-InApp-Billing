using System;
using System.Collections.Generic;

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

using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using IMarketBillingService = com.android.vending.billing.IMarketBillingService;
using Java.Lang;
using com.android.vending.billing;

namespace Billing
{
    /// <summary>
    /// This class sends messages to Android Market on behalf of the application by
    /// connecting (binding) to the MarketBillingService. The application
    /// creates an instance of this class and invokes billing requests through this service.
    /// 
    /// The <seealso cref="BillingReceiver"/> class starts this service to process commands
    /// that it receives from Android Market.
    /// 
    /// You should modify and obfuscate this code before using it.
    /// </summary>
    [Service]
    public class BillingService : Android.App.Service, Android.Content.IServiceConnection
    {
        private const string TAG = "BillingService";

        /// <summary>
        /// The service connection to the remote MarketBillingService. </summary>
        private static IMarketBillingService mService;

        /// <summary>
        /// The list of requests that are pending while we are waiting for the
        /// connection to the MarketBillingService to be established.
        /// </summary>
        private static LinkedList<BillingRequest> mPendingRequests = new LinkedList<BillingRequest>();

        /// <summary>
        /// The list of requests that we have sent to Android Market but for which we have
        /// not yet received a response code. The HashMap is indexed by the
        /// request Id that each request receives when it executes.
        /// </summary>
        private static Dictionary<long?, BillingRequest> mSentRequests = new Dictionary<long?, BillingRequest>();

        /// <summary>
        /// The base class for all requests that use the MarketBillingService.
        /// Each derived class overrides the run() method to call the appropriate
        /// service interface.  If we are already connected to the MarketBillingService,
        /// then we call the run() method directly. Otherwise, we bind
        /// to the service and save the request on a queue to be run later when
        /// the service is connected.
        /// </summary>
        public abstract class BillingRequest
        {
            private readonly BillingService outerInstance;

            private readonly int mStartId;
            protected internal long mRequestId;

            public BillingRequest(BillingService outerInstance, int startId)
            {
                this.outerInstance = outerInstance;
                mStartId = startId;
            }

            public virtual int StartId
            {
                get
                {
                    return mStartId;
                }
            }

            /// <summary>
            /// Run the request, starting the connection if necessary. </summary>
            /// <returns> true if the request was executed or queued; false if there
            /// was an error starting the connection </returns>
            public virtual bool RunRequest()
            {
                if (RunIfConnected())
                {
                    return true;
                }

                if (outerInstance.BindToMarketBillingService())
                {
                    // Add a pending request to run when the service is connected.
                    mPendingRequests.AddLast(this);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Try running the request directly if the service is already connected. </summary>
            /// <returns> true if the request ran successfully; false if the service
            /// is not connected or there was an error when trying to use it </returns>
            public virtual bool RunIfConnected()
            {
                if (Consts.DEBUG)
                {
                    Log.Debug(TAG, this.GetType().Name);
                }
                if (mService != null)
                {
                    try
                    {
                        mRequestId = Run();
                        if (Consts.DEBUG)
                        {
                            Log.Debug(TAG, "request id: " + mRequestId);
                        }
                        if (mRequestId >= 0)
                        {
                            mSentRequests[mRequestId] = this;
                        }
                        return true;
                    }
                    catch (RemoteException e)
                    {
                        OnRemoteException(e);
                    }
                }
                return false;
            }

            /// <summary>
            /// Called when a remote exception occurs while trying to execute the
            /// <seealso cref="#run()"/> method.  The derived class can override this to
            /// execute exception-handling code. </summary>
            /// <param name="e"> the exception </param>
            protected internal virtual void OnRemoteException(RemoteException e)
            {
                Log.Warn(TAG, "remote billing service crashed");
                mService = null;
            }

            /// <summary>
            /// The derived class must implement this method. </summary>
            /// <exception cref="RemoteException"> </exception>
            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: protected abstract long run() throws android.os.RemoteException;
            protected internal abstract long Run();

            /// <summary>
            /// This is called when Android Market sends a response code for this
            /// request. </summary>
            /// <param name="responseCode"> the response code </param>
            protected internal virtual void ResponseCodeReceived(Consts.ResponseCode responseCode)
            {
            }

            protected internal virtual Bundle MakeRequestBundle(string method)
            {
                Bundle request = new Bundle();
                request.PutString(Consts.BILLING_REQUEST_METHOD, method);
                request.PutInt(Consts.BILLING_REQUEST_API_VERSION, 2);
                request.PutString(Consts.BILLING_REQUEST_PACKAGE_NAME, this.outerInstance.PackageName);
                return request;
            }

            protected internal virtual void LogResponseCode(string method, Bundle response)
            {
                Consts.ResponseCode responseCode = (Consts.ResponseCode)response.GetInt(Consts.BILLING_RESPONSE_RESPONSE_CODE);
                if (Consts.DEBUG)
                {
                    Log.Error(TAG, method + " received " + responseCode.ToString());
                }
            }
        }

        /// <summary>
        /// Wrapper class that checks if in-app billing is supported.
        /// 
        /// Note: Support for subscriptions implies support for one-time purchases. However, the opposite
        /// is not true.
        /// 
        /// Developers may want to perform two checks if both one-time and subscription products are
        /// available.
        /// </summary>
        internal class CheckBillingSupported : BillingRequest
        {
            private readonly BillingService outerInstance;

            public string mProductType = null;

            /// <summary>
            /// Legacy contrustor
            /// 
            /// This constructor is provided for legacy purposes. Assumes the calling application will
            /// not be using any features not present in API v1, such as subscriptions.
            /// </summary>
            [Obsolete]
            public CheckBillingSupported(BillingService outerInstance)
                : base(outerInstance, -1)
            {
                this.outerInstance = outerInstance;
                // This object is never created as a side effect of starting this
                // service so we pass -1 as the startId to indicate that we should
                // not stop this service after executing this request.
            }

            /// <summary>
            /// Constructor
            /// 
            /// Note: Support for subscriptions implies support for one-time purchases. However, the
            /// opposite is not true.
            /// 
            /// Developers may want to perform two checks if both one-time and subscription products are
            /// available.
            /// 
            /// @pram itemType Either Consts.ITEM_TYPE_INAPP or Consts.ITEM_TYPE_SUBSCRIPTION, indicating
            /// the type of item support is being checked for.
            /// </summary>
            public CheckBillingSupported(BillingService outerInstance, string itemType)
                : base(outerInstance, -1)
            {
                this.outerInstance = outerInstance;
                mProductType = itemType;
            }

            protected internal override long Run()
            {
                try
                {
                    Bundle request = MakeRequestBundle("CHECK_BILLING_SUPPORTED");
                    if (mProductType != null)
                    {
                        request.PutString(Consts.BILLING_REQUEST_ITEM_TYPE, mProductType);
                    }
                    Bundle response = mService.SendBillingRequest(request);
                    int responseCode = response.GetInt(Consts.BILLING_RESPONSE_RESPONSE_CODE);
                    if (Consts.DEBUG)
                    {
                        Log.Info(TAG, "CheckBillingSupported response code: " + responseCode);
                    }
                    bool billingSupported = (responseCode == (int)Consts.ResponseCode.RESULT_OK);
                    ResponseHandler.CheckBillingSupportedResponse(billingSupported, mProductType);
                    return Consts.BILLING_RESPONSE_INVALID_REQUEST_ID;
                }
                catch (Android.OS.RemoteException)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Wrapper class that requests a purchase.
        /// </summary>
        public class RequestPurchase : BillingRequest
        {
            private readonly BillingService outerInstance;

            public readonly string mProductId;
            public readonly string mDeveloperPayload;
            public readonly string mProductType;

            /// <summary>
            /// Legacy constructor
            /// </summary>
            /// <param name="itemId">  The ID of the item to be purchased. Will be assumed to be a one-time
            ///                purchase. </param>
            [Obsolete]
            public RequestPurchase(BillingService outerInstance, string itemId)
                : this(outerInstance, itemId, null, null)
            {
                this.outerInstance = outerInstance;
            }

            /// <summary>
            /// Legacy constructor
            /// </summary>
            /// <param name="itemId">  The ID of the item to be purchased. Will be assumed to be a one-time
            ///                purchase. </param>
            /// <param name="developerPayload"> Optional data. </param>
            [Obsolete]
            public RequestPurchase(BillingService outerInstance, string itemId, string developerPayload)
                : this(outerInstance, itemId, null, developerPayload)
            {
                this.outerInstance = outerInstance;
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="itemId">  The ID of the item to be purchased. Will be assumed to be a one-time
            ///                purchase. </param>
            /// <param name="itemType">  Either Consts.ITEM_TYPE_INAPP or Consts.ITEM_TYPE_SUBSCRIPTION,
            ///                  indicating the type of item type support is being checked for. </param>
            /// <param name="developerPayload"> Optional data. </param>
            public RequestPurchase(BillingService outerInstance, string itemId, string itemType, string developerPayload)
                : base(outerInstance, -1)
            {
                this.outerInstance = outerInstance;
                // This object is never created as a side effect of starting this
                // service so we pass -1 as the startId to indicate that we should
                // not stop this service after executing this request.
                mProductId = itemId;
                mDeveloperPayload = developerPayload;
                mProductType = itemType;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: protected long run() throws android.os.RemoteException
            protected internal override long Run()
            {
                Bundle request = MakeRequestBundle("REQUEST_PURCHASE");
                request.PutString(Consts.BILLING_REQUEST_ITEM_ID, mProductId);
                request.PutString(Consts.BILLING_REQUEST_ITEM_TYPE, mProductType);
                // Note that the developer payload is optional.
                if (mDeveloperPayload != null)
                {
                    request.PutString(Consts.BILLING_REQUEST_DEVELOPER_PAYLOAD, mDeveloperPayload);
                }
                Bundle response = mService.SendBillingRequest(request);
                PendingIntent pendingIntent = (PendingIntent)response.GetParcelable(Consts.BILLING_RESPONSE_PURCHASE_INTENT);
                if (pendingIntent == null)
                {
                    Log.Error(TAG, "Error with requestPurchase");
                    return Consts.BILLING_RESPONSE_INVALID_REQUEST_ID;
                }

                Intent intent = new Intent();
                ResponseHandler.BuyPageIntentResponse(pendingIntent, intent);
                return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID, Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
            }

            protected internal override void ResponseCodeReceived(Consts.ResponseCode responseCode)
			{
				ResponseHandler.ResponseCodeReceived(this.outerInstance.ApplicationContext, this, responseCode);
			}
        }

        /// <summary>
        /// Wrapper class that confirms a list of notifications to the server.
        /// </summary>
        internal class ConfirmNotifications : BillingRequest
        {
            private readonly BillingService outerInstance;

            internal readonly string[] mNotifyIds;

            public ConfirmNotifications(BillingService outerInstance, int startId, string[] notifyIds)
                : base(outerInstance, startId)
            {
                this.outerInstance = outerInstance;
                mNotifyIds = notifyIds;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: protected long run() throws android.os.RemoteException
            protected internal override long Run()
            {
                Bundle request = MakeRequestBundle("CONFIRM_NOTIFICATIONS");
                request.PutStringArray(Consts.BILLING_REQUEST_NOTIFY_IDS, mNotifyIds);
                Bundle response = mService.SendBillingRequest(request);
                LogResponseCode("confirmNotifications", response);
                return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID, Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
            }
        }

        /// <summary>
        /// Wrapper class that sends a GET_PURCHASE_INFORMATION message to the server.
        /// </summary>
        public class GetPurchaseInformation : BillingRequest
        {
            private readonly BillingService outerInstance;

            internal long mNonce;
            internal readonly string[] mNotifyIds;

            public GetPurchaseInformation(BillingService outerInstance, int startId, string[] notifyIds)
                : base(outerInstance, startId)
            {
                this.outerInstance = outerInstance;
                mNotifyIds = notifyIds;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: protected long run() throws android.os.RemoteException
            protected internal override long Run()
            {
                mNonce = Security.GenerateNonce();

                Bundle request = MakeRequestBundle("GET_PURCHASE_INFORMATION");
                request.PutLong(Consts.BILLING_REQUEST_NONCE, mNonce);
                request.PutStringArray(Consts.BILLING_REQUEST_NOTIFY_IDS, mNotifyIds);
                Bundle response = mService.SendBillingRequest(request);
                LogResponseCode("getPurchaseInformation", response);
                return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID, Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
            }

            protected internal override void OnRemoteException(RemoteException e)
            {
                base.OnRemoteException(e);
                Security.RemoveNonce(mNonce);
            }
        }

        /// <summary>
        /// Wrapper class that sends a RESTORE_TRANSACTIONS message to the server.
        /// </summary>
        public class RestoreTransactions : BillingRequest
        {
            private readonly BillingService outerInstance;

            internal long mNonce;

            public RestoreTransactions(BillingService outerInstance)
                : base(outerInstance, -1)
            {
                this.outerInstance = outerInstance;
                // This object is never created as a side effect of starting this
                // service so we pass -1 as the startId to indicate that we should
                // not stop this service after executing this request.
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: protected long run() throws android.os.RemoteException
            protected internal override long Run()
            {
                mNonce = Security.GenerateNonce();

                Bundle request = MakeRequestBundle("RESTORE_TRANSACTIONS");
                request.PutLong(Consts.BILLING_REQUEST_NONCE, mNonce);
                Bundle response = mService.SendBillingRequest(request);
                LogResponseCode("restoreTransactions", response);
                return response.GetLong(Consts.BILLING_RESPONSE_REQUEST_ID, Consts.BILLING_RESPONSE_INVALID_REQUEST_ID);
            }

            protected internal override void OnRemoteException(RemoteException e)
            {
                base.OnRemoteException(e);
                Security.RemoveNonce(mNonce);
            }

            protected internal override void ResponseCodeReceived(Consts.ResponseCode responseCode)
			{
                ResponseHandler.ResponseCodeReceived(this.outerInstance.ApplicationContext, this, responseCode);
			}
        }

        public BillingService()
            : base()
        {
        }

        public virtual Context Context
        {
            set
            {
                AttachBaseContext(value);
            }
        }

        /// <summary>
        /// We don't support binding to this service, only starting the service.
        /// </summary>
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnStart(Intent intent, int startId)
        {
            HandleCommand(intent, startId);
        }

        /// <summary>
        /// The <seealso cref="BillingReceiver"/> sends messages to this service using intents.
        /// Each intent has an action and some extra arguments specific to that action. </summary>
        /// <param name="intent"> the intent containing one of the supported actions </param>
        /// <param name="startId"> an identifier for the invocation instance of this service </param>
        public virtual void HandleCommand(Intent intent, int startId)
        {
            string action = intent.Action;
            if (Consts.DEBUG)
            {
                Log.Info(TAG, "handleCommand() action: " + action);
            }
            if (Consts.ACTION_CONFIRM_NOTIFICATION.Equals(action))
            {
                string[] notifyIds = intent.GetStringArrayExtra(Consts.NOTIFICATION_ID);
                ConfirmNotificationsMethod(startId, notifyIds);
            }
            else if (Consts.ACTION_GET_PURCHASE_INFORMATION.Equals(action))
            {
                string notifyId = intent.GetStringExtra(Consts.NOTIFICATION_ID);
                GetPurchaseInformationMethod(startId, new string[] { notifyId });
            }
            else if (Consts.ACTION_PURCHASE_STATE_CHANGED.Equals(action))
            {
                string signedData = intent.GetStringExtra(Consts.INAPP_SIGNED_DATA);
                string signature = intent.GetStringExtra(Consts.INAPP_SIGNATURE);
                PurchaseStateChanged(startId, signedData, signature);
            }
            else if (Consts.ACTION_RESPONSE_CODE.Equals(action))
            {
                long requestId = intent.GetLongExtra(Consts.INAPP_REQUEST_ID, -1);
                int responseCodeIndex = intent.GetIntExtra(Consts.INAPP_RESPONSE_CODE, (int)Consts.ResponseCode.RESULT_ERROR);
                Consts.ResponseCode responseCode = (Consts.ResponseCode)responseCodeIndex;
                CheckResponseCode(requestId, responseCode);
            }
        }

        /// <summary>
        /// Binds to the MarketBillingService and returns true if the bind
        /// succeeded. </summary>
        /// <returns> true if the bind succeeded; false otherwise </returns>
        private bool BindToMarketBillingService()
        {
            try
            {
                if (Consts.DEBUG)
                {
                    Log.Info(TAG, "binding to Market billing service");
                }
                bool bindResult = BindService(new Intent(Consts.MARKET_BILLING_SERVICE_ACTION), this, Bind.AutoCreate); // ServiceConnection.

                if (bindResult)
                {
                    return true;
                }
                else
                {
                    Log.Error(TAG, "Could not bind to service.");
                }
            }
            catch (SecurityException e)
            {
                Log.Error(TAG, "Security exception: " + e);
            }
            return false;
        }

        /// <summary>
        /// Checks if in-app billing is supported. Assumes this is a one-time purchase.
        /// </summary>
        /// <returns> true if supported; false otherwise </returns>
        [Obsolete]
        public virtual bool CheckBillingSupportedMethod()
        {
            return new CheckBillingSupported(this).RunRequest();
        }

        /// <summary>
        /// Checks if in-app billing is supported.
        /// @pram itemType Either Consts.ITEM_TYPE_INAPP or Consts.ITEM_TYPE_SUBSCRIPTION, indicating the
        ///                type of item support is being checked for. </summary>
        /// <returns> true if supported; false otherwise </returns>
        public virtual bool CheckBillingSupportedMethod(string itemType)
        {
            return new CheckBillingSupported(this, itemType).RunRequest();
        }

        /// <summary>
        /// Requests that the given item be offered to the user for purchase. When
        /// the purchase succeeds (or is canceled) the <seealso cref="BillingReceiver"/>
        /// receives an intent with the action <seealso cref="Consts#ACTION_NOTIFY"/>.
        /// Returns false if there was an error trying to connect to Android Market. </summary>
        /// <param name="productId"> an identifier for the item being offered for purchase </param>
        /// <param name="itemType">  Either Consts.ITEM_TYPE_INAPP or Consts.ITEM_TYPE_SUBSCRIPTION, indicating
        ///                  the type of item type support is being checked for. </param>
        /// <param name="developerPayload"> a payload that is associated with a given
        /// purchase, if null, no payload is sent </param>
        /// <returns> false if there was an error connecting to Android Market </returns>
        public virtual bool RequestPurchaseMethod(string productId, string itemType, string developerPayload)
        {
            return new RequestPurchase(this, productId, itemType, developerPayload).RunRequest();
        }

        /// <summary>
        /// Requests transaction information for all managed items. Call this only when the
        /// application is first installed or after a database wipe. Do NOT call this
        /// every time the application starts up. </summary>
        /// <returns> false if there was an error connecting to Android Market </returns>
        public virtual bool RestoreTransactionsMethod()
        {
            return (new RestoreTransactions(this)).RunRequest();
        }

        /// <summary>
        /// Confirms receipt of a purchase state change. Each {@code notifyId} is
        /// an opaque identifier that came from the server. This method sends those
        /// identifiers back to the MarketBillingService, which ACKs them to the
        /// server. Returns false if there was an error trying to connect to the
        /// MarketBillingService. </summary>
        /// <param name="startId"> an identifier for the invocation instance of this service </param>
        /// <param name="notifyIds"> a list of opaque identifiers associated with purchase
        /// state changes. </param>
        /// <returns> false if there was an error connecting to Market </returns>
        private bool ConfirmNotificationsMethod(int startId, string[] notifyIds)
        {
            return (new ConfirmNotifications(this, startId, notifyIds)).RunRequest();
        }

        /// <summary>
        /// Gets the purchase information. This message includes a list of
        /// notification IDs sent to us by Android Market, which we include in
        /// our request. The server responds with the purchase information,
        /// encoded as a JSON string, and sends that to the <seealso cref="BillingReceiver"/>
        /// in an intent with the action <seealso cref="Consts#ACTION_PURCHASE_STATE_CHANGED"/>.
        /// Returns false if there was an error trying to connect to the MarketBillingService.
        /// </summary>
        /// <param name="startId"> an identifier for the invocation instance of this service </param>
        /// <param name="notifyIds"> a list of opaque identifiers associated with purchase
        /// state changes </param>
        /// <returns> false if there was an error connecting to Android Market </returns>
        private bool GetPurchaseInformationMethod(int startId, string[] notifyIds)
        {
            return (new GetPurchaseInformation(this, startId, notifyIds)).RunRequest();
        }

        /// <summary>
        /// Verifies that the data was signed with the given signature, and calls
        /// <seealso cref="ResponseHandler#purchaseResponse(Context, PurchaseState, String, String, long)"/>
        /// for each verified purchase. </summary>
        /// <param name="startId"> an identifier for the invocation instance of this service </param>
        /// <param name="signedData"> the signed JSON string (signed, not encrypted) </param>
        /// <param name="signature"> the signature for the data, signed with the private key </param>
        private void PurchaseStateChanged(int startId, string signedData, string signature)
        {
            List<Security.VerifiedPurchase> purchases;
            purchases = Security.VerifyPurchase(signedData, signature);
            if (purchases == null)
            {
                return;
            }

            List<string> notifyList = new List<string>();
            foreach (Security.VerifiedPurchase vp in purchases)
            {
                if (vp.notificationId != null)
                {
                    notifyList.Add(vp.notificationId);
                }
                ResponseHandler.PurchaseResponse(this, vp.purchaseState, vp.productId, vp.orderId, vp.purchaseTime, vp.developerPayload);
            }
            if (notifyList.Count > 0)
            {
                string[] notifyIds = notifyList.ToArray();
                ConfirmNotificationsMethod(startId, notifyIds);
            }
        }

        /// <summary>
        /// This is called when we receive a response code from Android Market for a request
        /// that we made. This is used for reporting various errors and for
        /// acknowledging that an order was sent to the server. This is NOT used
        /// for any purchase state changes.  All purchase state changes are received
        /// in the <seealso cref="BillingReceiver"/> and passed to this service, where they are
        /// handled in <seealso cref="#purchaseStateChanged(int, String, String)"/>. </summary>
        /// <param name="requestId"> a number that identifies a request, assigned at the
        /// time the request was made to Android Market </param>
        /// <param name="responseCode"> a response code from Android Market to indicate the state
        /// of the request </param>
        private void CheckResponseCode(long requestId, Consts.ResponseCode responseCode)
        {
            BillingRequest request = mSentRequests[requestId];
            if (request != null)
            {
                if (Consts.DEBUG)
                {
                    Log.Debug(TAG, request.GetType().Name + ": " + responseCode);
                }
                request.ResponseCodeReceived(responseCode);
            }
            mSentRequests.Remove(requestId);
        }

        /// <summary>
        /// Runs any pending requests that are waiting for a connection to the
        /// service to be established.  This runs in the main UI thread.
        /// </summary>
        private void RunPendingRequests()
        {
            int maxStartId = -1;
            BillingRequest request;
            while (mPendingRequests.First != null && mPendingRequests.First.Value != null)
            {
                request = mPendingRequests.First.Value;

                if (request.RunIfConnected())
                {
                    // Remove the request
                    mPendingRequests.RemoveFirst();

                    // Remember the largest startId, which is the most recent
                    // request to start this service.
                    if (maxStartId < request.StartId)
                    {
                        maxStartId = request.StartId;
                    }
                }
                else
                {
                    // The service crashed, so restart it. Note that this leaves
                    // the current request on the queue.
                    BindToMarketBillingService();
                    return;
                }
            }

            // If we get here then all the requests ran successfully.  If maxStartId
            // is not -1, then one of the requests started the service, so we can
            // stop it now.
            if (maxStartId >= 0)
            {
                if (Consts.DEBUG)
                {
                    Log.Info(TAG, "stopping service, startId: " + maxStartId);
                }
                StopSelf(maxStartId);
            }
        }

        /// <summary>
        /// This is called when we are connected to the MarketBillingService.
        /// This runs in the main UI thread.
        /// </summary>
        public /*override*/ void OnServiceConnected(ComponentName name, IBinder service)
        {
            if (Consts.DEBUG)
            {
                Log.Debug(TAG, "Billing service connected");
            }

            lock (this)
            {
                mService = BillingServiceStub.AsInterface(service);
                //mService = IMarketBillingService.Stub.asInterface(service);
                RunPendingRequests();
            }
        }

        /// <summary>
        /// This is called when we are disconnected from the MarketBillingService.
        /// </summary>
        public /*override*/ void OnServiceDisconnected(ComponentName name)
        {
            Log.Warn(TAG, "Billing service disconnected");
            mService = null;
        }

        /// <summary>
        /// Unbinds from the MarketBillingService. Call this when the application
        /// terminates to avoid leaking a ServiceConnection.
        /// </summary>
        public virtual void Unbind()
        {
            try
            {
                UnbindService(this);
            }
            catch (ArgumentException e)
            {
                // This might happen if the service was disconnected
            }
        }
    }
}