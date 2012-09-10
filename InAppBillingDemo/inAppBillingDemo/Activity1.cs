using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Billing;
using Android.Util;
using Android.Database;
using Java.Lang;
using System.Collections.Generic;

namespace inAppBillingDemo
{
    [Activity(Label = "inAppBillingDemo", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity
    {
        public TextView ItemName;

        public static string TAG = "inAppBillingDemo";
        private BillingService _billingService;
        private PurchaseDatabase _purchaseDatabase;
        private InAppBillingDemoPurchaseObserver _inAppBillingDemoObserver;
        private Handler _handler;
        public ICursor OwnedItemsCursor;
        private SimpleCursorAdapter _ownedItemsAdapter;
        public static string DB_INITIALIZED = "db_initialized";
        public HashSet<string> OwnedItems = new HashSet<string>();

        /**
         * Each product in the catalog can be MANAGED, UNMANAGED, or SUBSCRIPTION.  MANAGED
         * means that the product can be purchased only once per user (such as a new
         * level in a game). The purchase is remembered by Android Market and
         * can be restored if this application is uninstalled and then
         * re-installed. UNMANAGED is used for products that can be used up and
         * purchased multiple times (such as poker chips). It is up to the
         * application to keep track of UNMANAGED products for the user.
         * SUBSCRIPTION is just like MANAGED except that the user gets charged monthly
         * or yearly.
         */
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            Button buttonInApp = FindViewById<Button>(Resource.Id.InAppButton);
            ItemName = FindViewById<TextView>(Resource.Id.itemName);

            buttonInApp.Click += (o, e) => { InAppBilling(o, e); };
            StartBillingStuff();
            Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += new EventHandler<RaiseThrowableEventArgs>(AndroidEnvironment_UnhandledExceptionRaiser);
        }

        private void StartBillingStuff()
        {
            _handler = new Handler();
            _inAppBillingDemoObserver = new InAppBillingDemoPurchaseObserver(this, _handler);
            _billingService = new BillingService();
            _billingService.Context = this;
            ResponseHandler.Register(_inAppBillingDemoObserver);
            var inAppsSupported = _billingService.CheckBillingSupportedMethod(Consts.ITEM_TYPE_INAPP);
            var subscriptionsSupported = _billingService.CheckBillingSupportedMethod(Consts.ITEM_TYPE_SUBSCRIPTION);

            _purchaseDatabase = new PurchaseDatabase(this);
            OwnedItemsCursor = _purchaseDatabase.QueryAllPurchasedItems();
            StartManagingCursor(OwnedItemsCursor);
            var from = new string[] { PurchaseDatabase.PURCHASED_PRODUCT_ID_COL, PurchaseDatabase.PURCHASED_QUANTITY_COL };
            var to = new int[] { Resource.Id.itemName };
            _ownedItemsAdapter = new SimpleCursorAdapter(this, Resource.Layout.Main, OwnedItemsCursor, from, to);

            if (OwnedItems.Count == 0)
                ItemName.Text = "No purchases";
        }

        /**
         * If the database has not been initialized, we send a
         * RESTORE_TRANSACTIONS request to Android Market to get the list of purchased items
         * for this user. This happens if the application has just been installed
         * or the user wiped data. We do not want to do this on every startup, rather, we want to do
         * only when the database needs to be initialized.
         */
        public void RestoreDatabase()
        {
            var prefs = GetPreferences(FileCreationMode.Private);
            var initialized = prefs.GetBoolean(DB_INITIALIZED, false);

            if (!initialized)
            {
                _billingService.RestoreTransactionsMethod();
                Toast.MakeText(this, "Restoring transactions", ToastLength.Long).Show();
            }
        }

        /**
 * Creates a background thread that reads the database and initializes the
 * set of owned items.
 */
        private void InitializeOwnedItems()
        {
            new Thread(new Runnable(() =>
            {
                {
                    DoInitializeOwnedItems();
                }
            })).Start();
        }

        /**
 * Reads the set of purchased items from the database in a background thread
 * and then adds those items to the set of owned items in the main UI
 * thread.
 */
        private void DoInitializeOwnedItems()
        {
            ICursor cursor = _purchaseDatabase.QueryAllPurchasedItems();

            if (cursor == null)
            {
                return;
            }

            var ownedItems = new HashSet<string>();

            try
            {
                int productIdCol = cursor.GetColumnIndexOrThrow(PurchaseDatabase.PURCHASED_PRODUCT_ID_COL);

                while (cursor.MoveToNext())
                {
                    var productId = cursor.GetString(productIdCol);
                    ownedItems.Add(productId);
                    ItemName.Text = productId;
                }
            }
            finally
            {
                cursor.Close();
            }

            // We will add the set of owned items in a new Runnable that runs on
            // the UI thread so that we don't need to synchronize access to
            // mOwnedItems.
            _handler.Post(new Runnable(() =>
            {
                foreach (var item in ownedItems)
                {
                    OwnedItems.Add(item);
                }
            }));
        }

        private void AndroidEnvironment_UnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {

        }

        public void InAppBilling(object o, EventArgs e)
        {
            var worked = _billingService.RequestPurchaseMethod("android.test.purchased", Consts.ITEM_TYPE_INAPP, null);
            //var worked = _billingService.RequestPurchaseMethod("android.test.canceled", Consts.ITEM_TYPE_INAPP, null);
            //var worked = _billingService.RequestPurchaseMethod("android.test.refunded", Consts.ITEM_TYPE_INAPP, null);
            //var worked = _billingService.RequestPurchaseMethod("android.test.item_unavailable", Consts.ITEM_TYPE_INAPP, null);
        }

        protected override void OnStart()
        {
            base.OnStart();
            ResponseHandler.Register(_inAppBillingDemoObserver);
            InitializeOwnedItems();
        }

        protected override void OnStop()
        {
            base.OnStop();
            ResponseHandler.Unregister(_inAppBillingDemoObserver);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _purchaseDatabase.Close();
            _billingService.Unbind();
        }

        /**
         * Save the context of the log so simple things like rotation will not
         * result in the log being cleared.
         */
        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            //outState.putString(LOG_TEXT_KEY, Html.toHtml((Spanned)mLogTextView.getText()));
        }

        /**
         * Restore the contents of the log if it has previously been saved.
         */
        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);

            //if (savedInstanceState != null)
                //mLogTextView.setText(Html.fromHtml(savedInstanceState.getString(LOG_TEXT_KEY)));
        }
    }

    public class InAppBillingDemoPurchaseObserver : PurchaseObserver
    {
        private Activity1 _activity;

        public InAppBillingDemoPurchaseObserver(Activity1 activity, Handler handler)
            : base(activity, handler)
        {
            _activity = activity;
        }

        public override void OnBillingSupported(bool supported, string type)
        {
            if (Consts.DEBUG)
            {
                Log.Info(Activity1.TAG, "supported: " + supported);
            }
            if (type == null || type.Equals(Consts.ITEM_TYPE_INAPP))
            {
                if (supported)
                {
                    _activity.RestoreDatabase();
                }
                else
                {
                    // In app products not supported
                }
            }
            else if (type.Equals(Consts.ITEM_TYPE_SUBSCRIPTION))
            {
            }
            else
            {
                // Subscriptions not supported
            }
        }

        public override void OnPurchaseStateChange(Consts.PurchaseState purchaseState, string itemId,
                int quantity, long purchaseTime, string developerPayload)
        {
            if (Consts.DEBUG)
            {
                Log.Info(Activity1.TAG, "OnPurchaseStateChange() itemId: " + itemId + " " + purchaseState);
            }

            if (developerPayload == null)
            {
            }
            else
            {
            }

            if (purchaseState == Consts.PurchaseState.PURCHASED)
            {
                _activity.OwnedItems.Add(itemId);
                _activity.ItemName.Text = itemId;
            }

            if (purchaseState == Consts.PurchaseState.REFUNDED)
            {
            }

            if (purchaseState == Consts.PurchaseState.CANCELED)
            {
            }

            _activity.OwnedItemsCursor.Requery();
        }

        public override void OnRequestPurchaseResponse(BillingService.RequestPurchase request, Consts.ResponseCode responseCode)
        {
            if (Consts.DEBUG)
                Log.Debug(Activity1.TAG, request.mProductId + ": " + responseCode);

            if (responseCode == Consts.ResponseCode.RESULT_OK)
            {
                if (Consts.DEBUG)
                    Log.Info(Activity1.TAG, "purchase was successfully sent to server");
            }
            else if (responseCode == Consts.ResponseCode.RESULT_USER_CANCELED)
            {
                if (Consts.DEBUG)
                    Log.Info(Activity1.TAG, "user canceled purchase");
            }
            else
            {
                if (Consts.DEBUG)
                    Log.Info(Activity1.TAG, "purchase failed");
            }
        }

        public override void OnRestoreTransactionsResponse(BillingService.RestoreTransactions request, Consts.ResponseCode responseCode)
        {
            if (responseCode == Consts.ResponseCode.RESULT_OK)
            {
                if (Consts.DEBUG)
                    Log.Debug(Activity1.TAG, "completed RestoreTransactions request");

                // Update the shared preferences so that we don't perform
                // a RestoreTransactions again.
                ISharedPreferences prefs = _activity.GetPreferences(FileCreationMode.Private);
                ISharedPreferencesEditor edit = prefs.Edit();
                edit.PutBoolean(Activity1.DB_INITIALIZED, true);
                edit.Commit();
            }
            else
            {
                if (Consts.DEBUG)
                    Log.Debug(Activity1.TAG, "RestoreTransactions error: " + responseCode);
            }
        }
    }
}