// PostProcessMsi.js
//
// Performs a post-build fixup of an msi to prettify the setup wizard.
//
// Mon, 16 May 2011  21:43
//
// ==================================================================


var database = null;
var installer = null;

// Format a number as hex.  Quantities over 7ffffff will be displayed properly.
function decimalToHexString(number) {
    if (number < 0) {
        number = 0xFFFFFFFF + number + 1;
    }
    return number.toString(16).toUpperCase();
}


function LogException(loc, exc1) {
    WScript.StdErr.WriteLine("Exception [" + loc + "]");
    // for every property on the exception object
    for (var x in exc1) {
        if (x.toString() == "number") {
            WScript.StdErr.WriteLine("e[" + x + "] = 0x" + decimalToHexString(exc1[x]));
        }
        else {
            WScript.StdErr.WriteLine("e[" + x + "] = " + exc1[x]);
        }
    }
    WScript.Quit(1);
}


function updateText(dialog, control, text) {
    var sql = "UPDATE `Control` SET `Control`.`Text` = '" + text + "' "  +
        "WHERE `Control`.`Dialog_`='" + dialog + "' AND `Control`.`Control`='" + control + "'";
    var view = database.OpenView(sql);
    view.Execute();
    view.Close();
}

// function checkSqlError() {
//     var eRec = installer.LastErrorRecord();
//     if (eRec !== null) {
//         WScript.Echo("Fail: " + eRec.FormatText());
//         return false;
//     }
//     return true; // ok
// }

// function selectText(dialog, control) {
//     var sql = "Select  `Control`.`Text` from Control " +
//         "WHERE `Control`.`Dialog_`='" + dialog + "' AND `Control`.`Control`='" + control + "'";
//     WScript.Echo(sql);
//     WScript.Echo();
//     var view = database.OpenView(sql);
//     if (checkSqlError()) {
//         view.Execute();
//         var done = false;
//         var rCount = 0;
//         do {
//             var record = view.Fetch();
//             if (record === null) {
//                 done = true;
//             }
//             else {
//                 rCount++;
//                 var ccount = record.FieldCount;
//                 var delim = " ";
//                 var rowData = "";
//                 for (var i=0; i<ccount; i++) {
//                     if (i==ccount) { delim = "\n"; }
//                     rowData += record.StringData(i) + delim;
//                 }
//                 WScript.Echo(rowData);
//             }
//         } while (!done);
//         WScript.Echo(rCount + " rows.");
//     }
//     view.Close();
// }


// Constant values from Windows Installer
var msiOpenDatabaseModeTransact = 1;

if (WScript.Arguments.Length != 1) {
    WScript.StdErr.WriteLine(WScript.ScriptName + ": Updates an MSI to prettify it, and change some text.");
    WScript.StdErr.WriteLine("Usage: ");
    WScript.StdErr.WriteLine("  " + WScript.ScriptName + " <file>");
    WScript.Quit(1);
}

var filespec = WScript.Arguments(0);
WScript.Echo(WScript.ScriptName + " " + filespec);
var WshShell = new ActiveXObject("WScript.Shell");

try {
    installer = new ActiveXObject("WindowsInstaller.Installer");
    database = installer.OpenDatabase(filespec, msiOpenDatabaseModeTransact);
    // this will fail if Orca.exe has the same MSI already opened
}
catch (e1) {
    LogException("open database", e1);
}

if (database===null) {
    WScript.Echo("Failed.");
    WScript.Quit(1);
}


try {
    WScript.Echo("Beautifying the setup wizard...");

    // For some reason, the checkbox has a gray background instead of white or transparent.
    // I found no good explanation for this.
    // http://www.dizzymonkeydesign.com/blog/misc/adding-and-customizing-dlgs-in-wix-3/
    //
    // This step is a hack/workaround: it moves the checkbox to a gray area of the dialog
    //
    var sql = "UPDATE `Control` SET `Control`.`Height` = '18', `Control`.`Width` = '170', `Control`.`Y`='243', `Control`.`X`='10' "  +
        "WHERE `Control`.`Dialog_`='ExitDialog' AND `Control`.`Control`='OptionalCheckBox'";
    var view = database.OpenView(sql);
    view.Execute();
    view.Close();

    // The text on the exit dialog is too close to the title.  This
    // step moves the text down from Y=70 to Y=90, about one line.
    sql = "UPDATE `Control` SET `Control`.`Y` = '90' " +
        "WHERE `Control`.`Dialog_`='ExitDialog' AND `Control`.`Control`='Description'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();

    // The progressbar is too close to the status text on the Progress dialog.
    // This step moves the progressbar down from Y=115 to Y=118, about 1/3 line.
    sql = "UPDATE `Control` SET `Control`.`Y` = '118' " +
        "WHERE `Control`.`Dialog_`='ProgressDlg' AND `Control`.`Control`='ProgressBar'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();

    // The StatusLabel and ActionText controls are too short on the Progress dialog,
    // which means the bottom of the text is cut off.  This step
    // increases the height from 10 to 16.
    sql = "UPDATE `Control` SET `Control`.`Height` = '16' " +
        "WHERE `Control`.`Dialog_`='ProgressDlg' AND `Control`.`Control`='StatusLabel'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    sql = "UPDATE `Control` SET `Control`.`Height` = '16' " +
        "WHERE `Control`.`Dialog_`='ProgressDlg' AND `Control`.`Control`='ActionText'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();

    updateText("WelcomeDlg",
               "Title",
               "{\\WixUI_Font_Bigger}Welcome to the Setup Wizard for\r\n[ProductName]");

    updateText("ResumeDlg",
               "Title",
               "{\\WixUI_Font_Bigger}Welcome to the Setup Wizard for\r\n[ProductName]");

    updateText("MaintenanceWelcomeDlg",
               "Title",
               "{\\WixUI_Font_Bigger}Welcome to the Setup Wizard for\r\n[ProductName]");

    updateText("UserExit",
               "Title",
               "{\\WixUI_Font_Bigger}You interrupted the installation...");

    updateText("LicenseAgreementDlg",
               "Description",
               "Here`s the license agreement. Accept it.");

    updateText("ExitDialog",
               "Title",
               "{\\WixUI_Font_Bigger}The Setup Wizard for\r\n[ProductName]\r\nhas completed.");

    // Dialog_ : ExitDialog
    // Control : OptionalCheckBox
    // Text    : [WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT]
    //
    // Dialog_ : ExitDialog
    // Control : OptionalText
    // Text    : [WIXUI_EXITDIALOGOPTIONALTEXT]

    database.Commit();

    WScript.Echo("done.");
}
catch(e) {
    LogException("Editing", e);
}


