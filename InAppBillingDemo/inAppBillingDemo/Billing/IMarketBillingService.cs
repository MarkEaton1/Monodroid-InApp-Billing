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

using Android.OS;

namespace com.android.vending.billing
{
	internal interface IMarketBillingService : IInterface
	{
		/// <summary>
		/// Given the arguments in bundle form, returns a bundle for results. </summary>
		Bundle SendBillingRequest(Bundle bundle);
	}

    public abstract class BillingServiceStub : Binder, IMarketBillingService
    {
        private const string DESCRIPTOR = "com.android.vending.billing.IMarketBillingService";

        /** Construct the stub at attach it to the interface. */
        public BillingServiceStub()
        {
            this.AttachInterface(this, DESCRIPTOR);
        }
        /**
         * Cast an IBinder object into an IMarketBillingService interface,
         * generating a proxy if needed.
         */
        internal static IMarketBillingService AsInterface(IBinder obj)
        {
            if ((obj == null))
            {
                return null;
            }
            IInterface iin = (IInterface)obj.QueryLocalInterface(DESCRIPTOR);
            if (((iin != null) && (iin is IMarketBillingService)))
            {
                return ((IMarketBillingService)iin);
            }

            return new Proxy(obj);
        }

        public IBinder AsBinder()
        {
            return this;
        }

        protected override bool OnTransact(int code, Parcel data, Parcel reply, int flags)
        {
            switch (code)
            {
                case Binder.InterfaceConsts.InterfaceTransaction:
                    {
                        reply.WriteString(DESCRIPTOR);
                        return true;
                    }
                case TRANSACTION_checkBilling:
                    {
                        data.EnforceInterface(DESCRIPTOR);
                        Bundle _arg0;
                        _arg0 = data.ReadBundle();
                        this.SendBillingRequest(_arg0);
                        return true;
                    }
            }

            return base.OnTransact(code, data, reply, flags);
        }

        const int TRANSACTION_checkBilling = (Binder.InterfaceConsts.FirstCallTransaction + 0);
        public abstract Bundle SendBillingRequest(Bundle bundle);

        private class Proxy : Java.Lang.Object, IMarketBillingService
        {
            private IBinder mRemote;
            public Proxy(IBinder remote)
            {
                mRemote = remote;
            }
            public IBinder AsBinder()
            {
                return mRemote;
            }
            public string GetInterfaceDescriptor()
            {
                return DESCRIPTOR;
            }

            public Bundle SendBillingRequest(Bundle bundle)
            {
                Parcel _data = Parcel.Obtain();
                Parcel reply = Parcel.Obtain();
                Bundle replyBundle = null;
                bool bRes = false;

                try
                {
                    _data.WriteInterfaceToken(DESCRIPTOR);

                    if (bundle!=null) 
                    {
                        _data.WriteInt(1);
                        bundle.WriteToParcel(_data, ParcelableWriteFlags.None);
                    }
                    else 
                        _data.WriteInt(0);

                    bRes = mRemote.Transact(BillingServiceStub.TRANSACTION_checkBilling, _data, reply, TransactionFlags.None);

                    reply.ReadException();

                    if (reply.ReadInt() != 0)
                        replyBundle = Android.OS.Bundle.Creator.CreateFromParcel(reply) as Bundle;

                    return replyBundle;
                }
                catch (RemoteException e)
                {
                    var aaa = e.Message;
                    throw;
                }
                catch (Java.Lang.IllegalArgumentException e)
                {
                    var aaa = e.Message;
                    throw;
                }
                finally
                {
                    _data.Recycle();
                    reply.Recycle();
                }
            }
        }
    }
}
