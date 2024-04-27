using System.Collections.Generic;
using Vintagestory.API.Common;

namespace ServerAuth;

public static class Configuration
{
    private static int timeUntilKickUnloggedPlayer = 20000;
    private static bool freezeNonRegisteredPlayer = false;
    private static int maxAttemptsToBanPlayer = 5;
    private static int timeToReducePlayerAttempts = 60000;
    private static int timeToFreezeUnregisteredPlayersAfterJoin = 1000;
    private static string registerMessage = "This server is powered by authentication, consider protecting your account: /register password";
    private static string loginMessage = "To continue please login: /login password";
    private static string successRegisteredMessage = "Successfully registered the account, next time you login in you will need the password";
    private static string successLoggedMessage = "Successfully logged";
    private static string successChangedPasswordMessage = "Successfully changed your password";
    private static string errorTypePassword = "Please type a password";
    private static string errorAlreadyRegistered = "This account is already registered use /login password";
    private static string errorAlreadyLogged = "You are already logged";
    private static string errorNotRegistered = "This account is not registered yet, register using: /register password";
    private static string errorInvalidPassword = "Invalid password";
    private static string errorTooManyAttempts = "Too many attempts";
    private static string errorChangePasswordWithoutLogin = "You cannot change the password without login in";
    public static int TimeUntilKickUnloggedPlayer => timeUntilKickUnloggedPlayer;
    public static bool FreezeNonRegisteredPlayer => freezeNonRegisteredPlayer;
    public static int MaxAttemptsToBanPlayer => maxAttemptsToBanPlayer;
    public static int TimeToReducePlayerAttempts => timeToReducePlayerAttempts;
    public static int TimeToFreezeUnregisteredPlayersAfterJoin => timeToFreezeUnregisteredPlayersAfterJoin;
    public static string RegisterMessage => registerMessage;
    public static string LoginMessage => loginMessage;
    public static string SuccessRegisteredMessage => successRegisteredMessage;
    public static string SuccessLoggedMessage => successLoggedMessage;
    public static string SuccessChangedPasswordMessage => successChangedPasswordMessage;
    public static string ErrorTypePassword => errorTypePassword;
    public static string ErrorAlreadyRegistered => errorAlreadyRegistered;
    public static string ErrorAlreadyLogged => errorAlreadyLogged;
    public static string ErrorNotRegistered => errorNotRegistered;
    public static string ErrorInvalidPassword => errorInvalidPassword;
    public static string ErrorTooManyAttempts => errorTooManyAttempts;
    public static string ErrorChangePasswordWithoutLogin => errorChangePasswordWithoutLogin;

    public static void PopulateConfigurations(ICoreAPI api)
    {
        Dictionary<string, object> baseConfigs = api.Assets.Get(new AssetLocation("serverauth:config/base.json")).ToObject<Dictionary<string, object>>();
        { //timeUntilKickUnloggedPlayer
            if (baseConfigs.TryGetValue("timeUntilKickUnloggedPlayer", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: timeUntilKickUnloggedPlayer is null");
                else if (value is not long) Debug.Log($"CONFIGURATION ERROR: timeUntilKickUnloggedPlayer is not int is {value.GetType()}");
                else timeUntilKickUnloggedPlayer = (int)(long)value;
            else Debug.Log("CONFIGURATION ERROR: timeUntilKickUnloggedPlayer not set");
        }
        { //freezeNonRegisteredPlayer
            if (baseConfigs.TryGetValue("freezeNonRegisteredPlayer", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: freezeNonRegisteredPlayer is null");
                else if (value is not bool) Debug.Log($"CONFIGURATION ERROR: freezeNonRegisteredPlayer is not boolean is {value.GetType()}");
                else freezeNonRegisteredPlayer = (bool)value;
            else Debug.Log("CONFIGURATION ERROR: freezeNonRegisteredPlayer not set");
        }
        { //maxAttemptsToBanPlayer
            if (baseConfigs.TryGetValue("maxAttemptsToBanPlayer", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: maxAttemptsToBanPlayer is null");
                else if (value is not long) Debug.Log($"CONFIGURATION ERROR: maxAttemptsToBanPlayer is not int is {value.GetType()}");
                else maxAttemptsToBanPlayer = (int)(long)value;
            else Debug.Log("CONFIGURATION ERROR: maxAttemptsToBanPlayer not set");
        }
        { //timeToReducePlayerAttempts
            if (baseConfigs.TryGetValue("timeToReducePlayerAttempts", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: timeToReducePlayerAttempts is null");
                else if (value is not long) Debug.Log($"CONFIGURATION ERROR: timeToReducePlayerAttempts is not int is {value.GetType()}");
                else timeToReducePlayerAttempts = (int)(long)value;
            else Debug.Log("CONFIGURATION ERROR: timeToReducePlayerAttempts not set");
        }
        { //timeToFreezeUnregisteredPlayersAfterJoin
            if (baseConfigs.TryGetValue("timeToFreezeUnregisteredPlayersAfterJoin", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: timeToFreezeUnregisteredPlayersAfterJoin is null");
                else if (value is not long) Debug.Log($"CONFIGURATION ERROR: timeToFreezeUnregisteredPlayersAfterJoin is not int is {value.GetType()}");
                else timeToFreezeUnregisteredPlayersAfterJoin = (int)(long)value;
            else Debug.Log("CONFIGURATION ERROR: timeToFreezeUnregisteredPlayersAfterJoin not set");
        }
        { //registerMessage
            if (baseConfigs.TryGetValue("registerMessage", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: registerMessage is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: registerMessage is not string is {value.GetType()}");
                else registerMessage = (string)value;
            else Debug.Log("CONFIGURATION ERROR: registerMessage not set");
        }
        { //loginMessage
            if (baseConfigs.TryGetValue("loginMessage", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: loginMessage is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: loginMessage is not string is {value.GetType()}");
                else loginMessage = (string)value;
            else Debug.Log("CONFIGURATION ERROR: loginMessage not set");
        }
        { //successRegisteredMessage
            if (baseConfigs.TryGetValue("successRegisteredMessage", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: successRegisteredMessage is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: successRegisteredMessage is not string is {value.GetType()}");
                else successRegisteredMessage = (string)value;
            else Debug.Log("CONFIGURATION ERROR: successRegisteredMessage not set");
        }
        { //successLoggedMessage
            if (baseConfigs.TryGetValue("successLoggedMessage", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: successLoggedMessage is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: successLoggedMessage is not string is {value.GetType()}");
                else successLoggedMessage = (string)value;
            else Debug.Log("CONFIGURATION ERROR: successLoggedMessage not set");
        }
        { //successChangedPasswordMessage
            if (baseConfigs.TryGetValue("successChangedPasswordMessage", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: successChangedPasswordMessage is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: successChangedPasswordMessage is not string is {value.GetType()}");
                else successChangedPasswordMessage = (string)value;
            else Debug.Log("CONFIGURATION ERROR: successChangedPasswordMessage not set");
        }
        { //errorTypePassword
            if (baseConfigs.TryGetValue("errorTypePassword", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: errorTypePassword is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: errorTypePassword is not string is {value.GetType()}");
                else errorTypePassword = (string)value;
            else Debug.Log("CONFIGURATION ERROR: errorTypePassword not set");
        }
        { //errorAlreadyRegistered
            if (baseConfigs.TryGetValue("errorAlreadyRegistered", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: errorAlreadyRegistered is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: errorAlreadyRegistered is not string is {value.GetType()}");
                else errorAlreadyRegistered = (string)value;
            else Debug.Log("CONFIGURATION ERROR: errorAlreadyRegistered not set");
        }
        { //errorAlreadyLogged
            if (baseConfigs.TryGetValue("errorAlreadyLogged", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: errorAlreadyLogged is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: errorAlreadyLogged is not string is {value.GetType()}");
                else errorAlreadyLogged = (string)value;
            else Debug.Log("CONFIGURATION ERROR: errorAlreadyLogged not set");
        }
        { //errorNotRegistered
            if (baseConfigs.TryGetValue("errorNotRegistered", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: errorNotRegistered is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: errorNotRegistered is not string is {value.GetType()}");
                else errorNotRegistered = (string)value;
            else Debug.Log("CONFIGURATION ERROR: errorNotRegistered not set");
        }
        { //errorInvalidPassword
            if (baseConfigs.TryGetValue("errorInvalidPassword", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: errorInvalidPassword is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: errorInvalidPassword is not string is {value.GetType()}");
                else errorInvalidPassword = (string)value;
            else Debug.Log("CONFIGURATION ERROR: errorInvalidPassword not set");
        }
        { //errorTooManyAttempts
            if (baseConfigs.TryGetValue("errorTooManyAttempts", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: errorTooManyAttempts is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: errorTooManyAttempts is not string is {value.GetType()}");
                else errorTooManyAttempts = (string)value;
            else Debug.Log("CONFIGURATION ERROR: errorTooManyAttempts not set");
        }
        { //errorChangePasswordWithoutLogin
            if (baseConfigs.TryGetValue("errorChangePasswordWithoutLogin", out object value))
                if (value is null) Debug.Log("CONFIGURATION ERROR: errorChangePasswordWithoutLogin is null");
                else if (value is not string) Debug.Log($"CONFIGURATION ERROR: errorChangePasswordWithoutLogin is not string is {value.GetType()}");
                else errorChangePasswordWithoutLogin = (string)value;
            else Debug.Log("CONFIGURATION ERROR: errorChangePasswordWithoutLogin not set");
        }

        Debug.Log("Authentication configurations set");
    }
}
