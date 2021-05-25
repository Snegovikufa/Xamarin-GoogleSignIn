using System.Diagnostics;
using System.Threading.Tasks;
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

#nullable enable

namespace SigninQuickstart
{
    [Activity(MainLauncher = true)]
    [Register("com.xamarin.signinquickstart.MainActivity")]
    public class MainActivity : Activity, View.IOnClickListener, GoogleApiClient.IOnConnectionFailedListener
    {
        private const string Tag = nameof(MainActivity);

        private const int RC_SIGN_IN = 9001;

        private GoogleSignInClient? _googleApiClient;
        private ProgressDialog? _progressDialog;
        private GoogleSignInClient GoogleApiClient => _googleApiClient ??= CreateGoogleApiClient();

        private TextView StatusTextView => FindViewById<TextView>(Resource.Id.status)!;
        public View SignOutButton => FindViewById(Resource.Id.sign_out_button)!;
        public SignInButton SignInButton => FindViewById<SignInButton>(Resource.Id.sign_in_button)!;

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

        protected override void OnStart()
        {
            base.OnStart();

            ShowProgressDialog();
        }

        protected override void OnResume()
        {
            base.OnResume();
            HideProgressDialog();
        }

        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            Log.Debug(Tag, "onActivityResult:" + requestCode + ":" + resultCode + ":" + data);

            if (requestCode == RC_SIGN_IN)
            {
                if (resultCode == Result.Ok)
                {
                    var result = await GoogleSignIn.GetSignedInAccountFromIntent(data);
                    HandleSignInResult(result.JavaCast<GoogleSignInAccount>());
                }
                else if (resultCode == Result.Canceled)
                {
                    UpdateUi(false);
                }
            }
        }

        public void HandleSignInResult(GoogleSignInAccount result)
        {
            StatusTextView.Text = string.Format(GetString(Resource.String.signed_in_fmt), result.DisplayName);
            UpdateUi(true);
        }

        private void SignIn()
        {
            var signInIntent = GoogleApiClient.SignInIntent;
            StartActivityForResult(signInIntent, RC_SIGN_IN);
        }

        private async Task SignOut()
        {
            await GoogleApiClient.SignOutAsync();
            UpdateUi(false);
        }

        public void ShowProgressDialog()
        {
            if (_progressDialog == null)
            {
                _progressDialog = new ProgressDialog(this);
                _progressDialog.SetMessage(GetString(Resource.String.loading));
                _progressDialog.Indeterminate = true;
            }

            _progressDialog.Show();
        }

        public void HideProgressDialog()
        {
            if (_progressDialog is { IsShowing: true })
            {
                _progressDialog.Hide();
            }
        }

        public void UpdateUi(bool isSignedIn)
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