using System;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UniRx;

namespace Skaillz.Ubernet.Providers.Photon
{
    internal class PhotonUtils
    {
        internal static DisconnectReason ConvertPhotonDisconnectCause(DisconnectCause cause)
        {
            switch (cause)
            {
                case DisconnectCause.MaxCcuReached:
                    return DisconnectReason.ExceededLimits;
                case DisconnectCause.Exception:
                case DisconnectCause.ExceptionOnConnect:
                case DisconnectCause.OperationNotAllowedInCurrentState:
                case DisconnectCause.InvalidRegion:
                    return DisconnectReason.Exception;
                case DisconnectCause.ServerTimeout:
                case DisconnectCause.DisconnectByServerLogic:
                    return DisconnectReason.DisconnectedByServer;
                case DisconnectCause.ClientTimeout:
                    return DisconnectReason.Timeout;
                case DisconnectCause.InvalidAuthentication:
                case DisconnectCause.CustomAuthenticationFailed:
                case DisconnectCause.AuthenticationTicketExpired:
                    return DisconnectReason.Unauthorized;
                default:
                    return DisconnectReason.Unknown;
            }
        }
        
        internal static IObservable<T> CreateObservableForExpectedStateChange<T>(LoadBalancingClient photonClient, 
            ClientState expectedState, T returnValue)
        {
            return Observable.Create<T>(observer =>
            {
                photonClient.OpResponseReceived += OpResponseAction;
                photonClient.StateChanged += StateChangeAction;
                
                void OpResponseAction(OperationResponse response)
                {
                    if (response.ReturnCode != 0)
                    {
                        observer.OnError(CreateExceptionForPhotonError(response));
                        observer.OnCompleted();
                    }
                }

                void StateChangeAction(ClientState oldState, ClientState newState)
                {
                    if (newState == expectedState)
                    {
                        observer.OnNext(returnValue);
                        observer.OnCompleted();
                    }
                    else if (newState == ClientState.Disconnected)
                    {
                        var reason = ConvertPhotonDisconnectCause(photonClient.DisconnectedCause);
                        observer.OnError(new ConnectionException("Disconnected from Photon.", reason));
                        observer.OnCompleted();
                    }
                }

                return Disposable.Create(() =>
                {
                    photonClient.OpResponseReceived -= OpResponseAction;
                    photonClient.StateChanged -= StateChangeAction;
                });
            });
        }
        
        internal static Exception CreateExceptionForPhotonError(OperationResponse response)
        {
            int errorCode = response.ReturnCode;
            switch (errorCode)
            {
                case ErrorCode.MaxCcuReached:
                    return new ServerFullException("The max CCU of the Photon instance have been exceeded.");
                case ErrorCode.InvalidAuthentication:
                    return new PhotonException("Invalid authentication. Check your Photon App ID.", errorCode);
                case ErrorCode.GameDoesNotExist:
                    return new GameDoesNotExistException();
                case ErrorCode.GameFull:
                    return new GameFullException();
                case ErrorCode.GameClosed:
                    return new PhotonGameJoinException("The game is closed.", errorCode);
                case ErrorCode.NoRandomMatchFound:
                    return new NoRandomGameFoundException();
                case ErrorCode.JoinFailedFoundActiveJoiner:
                case ErrorCode.JoinFailedFoundInactiveJoiner:
                case ErrorCode.JoinFailedPeerAlreadyJoined:
                case ErrorCode.JoinFailedFoundExcludedUserId:
                case ErrorCode.JoinFailedWithRejoinerNotFound:
                    return new PhotonGameJoinException($"Photon error with code {errorCode}: '{response.DebugMessage}'", errorCode);
                default:
                    return new PhotonException($"Photon error with code {errorCode}: '{response.DebugMessage}'", errorCode);
            }
        }
    }
}