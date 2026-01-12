namespace Driver;

/// <summary>
/// States for the login state machine.
/// </summary>
public enum LoginState
{
    /// <summary>
    /// Initial state - welcome banner shown.
    /// </summary>
    Welcome,

    /// <summary>
    /// Waiting for username or "new" command.
    /// </summary>
    AwaitingName,

    /// <summary>
    /// Waiting for password (existing user login).
    /// </summary>
    AwaitingPassword,

    /// <summary>
    /// Registration: waiting for new username.
    /// </summary>
    RegistrationName,

    /// <summary>
    /// Registration: waiting for email address.
    /// </summary>
    RegistrationEmail,

    /// <summary>
    /// Registration: waiting for password.
    /// </summary>
    RegistrationPassword,

    /// <summary>
    /// Registration: waiting for password confirmation.
    /// </summary>
    RegistrationConfirm,

    /// <summary>
    /// Successfully authenticated, transitioning to game.
    /// </summary>
    Authenticated,

    /// <summary>
    /// Waiting for confirmation to take over existing active session.
    /// </summary>
    ConfirmTakeover,

    /// <summary>
    /// In-game, player object exists.
    /// </summary>
    Playing
}
