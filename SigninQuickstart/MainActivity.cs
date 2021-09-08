using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Android.Accounts;
using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Extensions;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Util.Concurrent;

#nullable enable

namespace SigninQuickstart
{
    [Activity(MainLauncher = true)]
    [Register("com.xamarin.signinquickstart.MainActivity")]
    public class MainActivity : Activity, View.IOnClickListener, GoogleApiClient.IOnConnectionFailedListener
    {
        private const string Tag = nameof(MainActivity);

        private const int RcSignIn = 9001;

        private GoogleSignInClient? _googleApiClient;
        private GoogleSignInClient GoogleApiClient => _googleApiClient ??= CreateGoogleApiClient();

        private TextView StatusTextView => FindViewById<TextView>(Resource.Id.status)!;
        private View SignOutButton => FindViewById(Resource.Id.sign_out_button)!;
        private SignInButton SignInButton => FindViewById<SignInButton>(Resource.Id.sign_in_button)!;

        public async void OnClick(View? v)
        {
            switch (v?.Id)
            {
                case Resource.Id.sign_in_button:
                    SignIn();
                    break;
                case Resource.Id.sign_out_button:
                    await SignOut();
                    break;
            }
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            Log.Debug(Tag, "onConnectionFailed:" + result);
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var serverClientId = Application.Context.Resources!.GetString(Resource.String.server_client_id);
            if (serverClientId == "YOUR_SERVER_CLIENT_ID")
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                else
                {
                    Toast.MakeText(this, "Please set your server_client_id", ToastLength.Long);
                }
            }

            SetContentView(Resource.Layout.activity_main);

            SignInButton.SetOnClickListener(this);
            SignOutButton.SetOnClickListener(this);

            CreateGoogleApiClient();

            SignInButton.SetSize(SignInButton.SizeStandard);
        }

        private GoogleSignInClient CreateGoogleApiClient()
        {
            var options = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                .RequestEmail()
                .RequestProfile()
                .Build();

            return GoogleSignIn.GetClient(this, options);
        }

        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            Log.Debug(Tag, "onActivityResult:" + requestCode + ":" + resultCode + ":" + data);

            if (requestCode == RcSignIn)
            {
                if (resultCode == Result.Ok)
                {
                    var result = await GoogleSignIn.GetSignedInAccountFromIntent(data);
                    await HandleSignInResult(result.JavaCast<GoogleSignInAccount>());
                }
                else if (resultCode == Result.Canceled)
                {
                    UpdateUi(false);
                }
            }
        }

        private async Task HandleSignInResult(GoogleSignInAccount result)
        {
            StatusTextView.Text = string.Format(GetString(Resource.String.signed_in_fmt), result.DisplayName);

            using var accountManager = AccountManager.Get(Application.Context);
            using var accountManagerFuture = accountManager!.GetAuthToken(result.Account, GetScope(),
                null,
                this,
                null,
                null);
            using var tokenBundle = await accountManagerFuture!.GetResultAsync(2, TimeUnit.Minutes);
            using var bundle = tokenBundle.JavaCast<Bundle>();
            var authToken = bundle!.GetString(AccountManager.KeyAuthtoken);
            Toast.MakeText(ApplicationContext, authToken, ToastLength.Short);

            UpdateUi(true);
        }

        private static string GetScope()
        {
            var scopes = new[] { Scopes.Profile, Scopes.Email };
            return $"oauth2:{string.Join(" ", scopes)}";
        }

        private void SignIn()
        {
            var signInIntent = GoogleApiClient.SignInIntent;
            StartActivityForResult(signInIntent, RcSignIn);
        }

        private async Task SignOut()
        {
            await GoogleApiClient.SignOutAsync();
            UpdateUi(false);
        }

        private void UpdateUi(bool isSignedIn)
        {
            if (isSignedIn)
            {
                SignInButton.Visibility = ViewStates.Gone;
                SignOutButton.Visibility = ViewStates.Visible;
            }
            else
            {
                StatusTextView.Text = GetString(Resource.String.signed_out);

                SignInButton.Visibility = ViewStates.Visible;
                SignOutButton.Visibility = ViewStates.Gone;
            }
        }
    }
}