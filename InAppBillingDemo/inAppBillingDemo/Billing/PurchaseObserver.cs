using System;

// Copyright 2010 Google Inc. All Rights Reserved.

using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;

namespace Billing
{
    /// <summary>
    /// An interface for observing changes related to purchases. The main application
    /// extends this class and registers an instance of that derived class with
    /// <seealso cref="ResponseHandler"/>. The main application implements the callbacks
    /// <seealso cref="#onBillingSupported(boolean)"/> and
    /// <seealso cref="#onPurchaseStateChange(PurchaseState, String, int, long)"/>.  These methods
    /// are used to update the UI.
    /// </summary>
    public abstract class PurchaseObserver
    {
        private const string TAG = "PurchaseObserver";
        private readonly Activity mActivity;
        private readonly Handler mHandler;
        private System.Reflection.MethodInfo mStartIntentSender;
        private object[] mStartIntentSenderArgs = new object[5];
        private static readonly Type[] START_INTENT_SENDER_SIG = new Type[] { typeof(IntentSender), typeof(Intent), typeof(ActivityFlags), typeof(ActivityFlags), typeof(int) };

        public PurchaseObserver(Activity activity, Handler handler)
        {
            mActivity = activity;
            mHandler = handler;
            InitCompatibilityLayer();
        }

        /// <summary>
        /// This is the callback that is invoked when Android Market responds to the
        /// <seealso cref="BillingService#checkBillingSupported()"/> request. </summary>
        /// <param name="supported"> true if in-app billing is supported. </param>
        public abstract void OnBillingSupported(bool supported, string type);

        /// <summary>
        /// This is the callback that is invoked when an item is purchased,
        /// refunded, or canceled.  It is the callback invoked in response to
        /// calling <seealso cref="BillingService#requestPurchase(String)"/>.  It may also
        /// be invoked asynchronously when a purchase is made on another device
        /// (if the purchase was for a Market-managed item), or if the purchase
        /// was refunded, or the charge was canceled.  This handles the UI
        /// update.  The database update is handled in
        /// {@link ResponseHandler#purchaseResponse(Context, PurchaseState,
        /// String, String, long)}. </summary>
        /// <param name="purchaseState"> the purchase state of the item </param>
        /// <param name="itemId"> a string identifying the item (the "SKU") </param>
        /// <param name="quantity"> the current quantity of this item after the purchase </param>
        /// <param name="purchaseTime"> the time the product was purchased, in
        /// milliseconds since the epoch (Jan 1, 1970) </param>
        public abstract void OnPurchaseStateChange(Consts.PurchaseState purchaseState, string itemId, int quantity, long purchaseTime, string developerPayload);

        /// <summary>
        /// This is called when we receive a response code from Market for a
        /// RequestPurchase request that we made.  This is NOT used for any
        /// purchase state changes.  All purchase state changes are received in
        /// <seealso cref="#onPurchaseStateChange(PurchaseState, String, int, long)"/>.
        /// This is used for reporting various errors, or if the user backed out
        /// and didn't purchase the item.  The possible response codes are:
        ///   RESULT_OK means that the order was sent successfully to the server.
        ///       The onPurchaseStateChange() will be invoked later (with a
        ///       purchase state of PURCHASED or CANCELED) when the order is
        ///       charged or canceled.  This response code can also happen if an
        ///       order for a Market-managed item was already sent to the server.
        ///   RESULT_USER_CANCELED means that the user didn't buy the item.
        ///   RESULT_SERVICE_UNAVAILABLE means that we couldn't connect to the
        ///       Android Market server (for example if the data connection is down).
        ///   RESULT_BILLING_UNAVAILABLE means that in-app billing is not
        ///       supported yet.
        ///   RESULT_ITEM_UNAVAILABLE means that the item this app offered for
        ///       sale does not exist (or is not published) in the server-side
        ///       catalog.
        ///   RESULT_ERROR is used for any other errors (such as a server error).
        /// </summary>
        public abstract void OnRequestPurchaseResponse(BillingService.RequestPurchase request, Consts.ResponseCode responseCode);

        /// <summary>
        /// This is called when we receive a response code from Android Market for a
        /// RestoreTransactions request that we made.  A response code of
        /// RESULT_OK means that the request was successfully sent to the server.
        /// </summary>
        public abstract void OnRestoreTransactionsResponse(BillingService.RestoreTransactions request, Consts.ResponseCode responseCode);

        private void InitCompatibilityLayer()
        {
            try
            {
                mStartIntentSender = mActivity.GetType().GetMethod("StartIntentSender", START_INTENT_SENDER_SIG);
            }
            catch (Java.Lang.SecurityException e)
            {
                mStartIntentSender = null;
            }
            catch (Java.Lang.NoSuchMethodException e)
            {
                mStartIntentSender = null;
            }
        }

        internal virtual void StartBuyPageActivity(PendingIntent pendingIntent, Intent intent)
        {
            //if (mStartIntentSender != null)
            {
                // This is on Android 2.0 and beyond.  The in-app buy page activity
                // must be on the activity stack of the application.
                try
                {
                    // This implements the method call: mActivity.StartIntentSender(pendingIntent.IntentSender, intent, 0, 0, 0);

                    mActivity.StartIntentSender(pendingIntent.IntentSender, intent, 0, 0, 0);


         //           mStartIntentSenderArgs[0] = pendingIntent.IntentSender;
           //         mStartIntentSenderArgs[1] = intent;
             //       mStartIntentSenderArgs[2] = Convert.ToInt32(0);
               //     mStartIntentSenderArgs[3] = Convert.ToInt32(0);
                 //   mStartIntentSenderArgs[4] = Convert.ToInt32(0);
                   // mStartIntentSender.Invoke(mActivity, mStartIntentSenderArgs);
                }
                catch (Exception e)
                {
                    Log.Error(TAG, "error starting activity", e);
                }
            }
         //   else
           // {
                // This is on Android version 1.6. The in-app buy page activity must be on its
                // own separate activity stack instead of on the activity stack of
                // the application.
    //            try
      //          {
           //         pendingIntent.Send(mActivity, 0, intent); // code
        //        }
          //      catch (PendingIntent.CanceledException e)
            //    {
              //      Log.Error(TAG, "error starting activity", e);
                //}
         //   }
        }

        /// <summary>
        /// Updates the UI after the database has been updated.  This method runs
        /// in a background thread so it has to post a Runnable to run on the UI
        /// thread. </summary>
        /// <param name="purchaseState"> the purchase state of the item </param>
        /// <param name="itemId"> a string identifying the item </param>
        /// <param name="quantity"> the quantity of items in this purchase </param>
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
        //ORIGINAL LINE: void postPurchaseStateChange(final com.example.dungeons.Consts.PurchaseState purchaseState, final String itemId, final int quantity, final long purchaseTime, final String developerPayload)
        internal virtual void PostPurchaseStateChange(Consts.PurchaseState purchaseState, string itemId, int quantity, long purchaseTime, string developerPayload)
        {
            mHandler.Post(() =>
                    {
                        OnPurchaseStateChange(purchaseState, itemId, quantity, purchaseTime, developerPayload);
                    });
        }
    }
}