using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E_mail_Net_Disk.Mail
{
    //http://www.ietf.org/rfc/rfc2821.txt
    public enum SmtpCode : int
    {
        None = 0,
        SystemStatus = 211,
        HelpMessage = 214,
        ServiceReady = 220,
        ServiceClosingTransmissionChannel = 221,
        AuthenticationSuccessful = 235,
        RequestedMailActionCompleted = 250,
        UserNotLocalWillForwardTo = 251,
        CannotVerifyUserButWillAcceptMessage = 252,
        WaitingForAuthentication = 334,
        StartMailInput = 354,
        ServiceNotAvailable = 421,
        MailboxBusy = 450,
        RequestedError = 451,
        InsufficientSystemStorage = 452,
        SyntaxError = 500,
        SyntaxErrorInParameters = 501,
        CommandNotImplemented = 502,
        BadSequenceCommand = 503,
        CommandParameterNotImplemented = 504,
        MailboxUnavailable = 550,
        UserNotLocalTryOther = 551,
        ExceededStorageAllocation = 552,
        MailboxNameNotAllowed = 553,
        TransactionFailed = 554,

    }
}
