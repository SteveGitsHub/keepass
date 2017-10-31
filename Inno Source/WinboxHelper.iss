[Setup]
AppName="Winbox Helper"
AppVersion=1.0
DefaultDirName={sd}\WinboxHelper
DisableProgramGroupPage=yes
DisableDirPage=yes
DisableWelcomePage=yes

[Files]
Source: "WinboxHelper.exe"; DestDir: "{app}"
Source: "helpers.xml"; DestDir: "{app}"
Source: "KeePassLib.dll"; DestDir: "{app}"
Source: "README.docx"; DestDir: "{app}"; Flags: isreadme

[Code]
var
  DataFilePage: TInputFileWizardPage;

procedure InitializeWizard;
begin
      DataFilePage := CreateInputFilePage(wpSelectDir, '', '', '');
      DataFilePage.Add('Enter the location of your KeePass .kdbx file:', 'KeePass db|*.kdbx|All files|*.*', '.kdbx');
      DataFilePage.Add('Enter the location of Winbox.exe on your computer:', 'Executable files|*.exe|All files|*.*', '.exe');
end;

function GetDataFile(Param: String): String;
begin
  if Param = 'KeePass' then begin
  Result := DataFilePage.Values[0];
  end
  else if Param = 'Winbox' then begin
  Result := DataFilePage.Values[1];
  end
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
    if CurPageID = DataFilePage.ID then begin
      if (False) then begin
        MsgBox('This is the folder picker.', mbError, MB_OK);
        Result := False;
      end
      else if (Pos('.kdbx', DataFilePage.Values[0]) <= 0) then begin
        MsgBox('kdbx file not found in first parameter.', mbError, MB_OK);
        Result := False;
      end else if(Pos('Winbox.exe', DataFilePage.Values[1]) <= 0) and (Pos('winbox.exe', DataFilePage.Values[1]) <= 0)
      and (Pos('WINBOX.exe', DataFilePage.Values[1]) <= 0) then begin
        MsgBox('Winbox.exe not found in second parameter.', mbError, MB_OK);
        Result := False;
      end else
        Result := True;
    end else
    Result := True;
end;

[Registry]
Root: HKCR; Subkey: "WinboxHelper"; ValueType: string; ValueData: "URL:winboxhelper Protocol"; Flags: uninsdeletekey
Root: HKCR; Subkey: "WinboxHelper"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Flags: uninsdeletekey
Root: HKCR; Subkey: "WinboxHelper\Shell"; Flags: uninsdeletekey
Root: HKCR; Subkey: "WinboxHelper\Shell\Open"; Flags: uninsdeletekey
Root: HKCR; Subkey: "WinboxHelper\Shell\Open\Command"; ValueType: string; ValueData: """C:\WinboxHelper\WinboxHelper.exe"" ""%1"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\WinboxHelper"; ValueType: string; ValueName: "KeePass Location"; ValueData: "{code:GetDataFile|KeePass}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\WinboxHelper"; ValueType: string; ValueName: "Winbox Location"; ValueData: "{code:GetDataFile|Winbox}"; Flags: uninsdeletekey
