; -- CodeDependencies.iss --
;
; This script shows how to download and install any dependency such as .NET,
; Visual C++ or SQL Server during your application's installation process.
;
; downloaded from: https://github.com/DomGries/InnoDependencyInstaller


; -----------
; SHARED CODE
; -----------
[Code]
// types and variables
type
  TDependency_Entry = record
    Filename: String;
    Parameters: String;
    Title: String;
    URL: String;
    Checksum: String;
    ForceSuccess: Boolean;
    RestartAfter: Boolean;
  end;

var
  Dependency_Memo: String;
  Dependency_List: array of TDependency_Entry;
  Dependency_NeedRestart, Dependency_ForceX86: Boolean;
  Dependency_DownloadPage: TDownloadWizardPage;

procedure Dependency_Add(const Filename, Parameters, Title, URL, Checksum: String; const ForceSuccess, RestartAfter: Boolean);
var
  Dependency: TDependency_Entry;
  DependencyCount: Integer;
begin
  Dependency_Memo := Dependency_Memo + #13#10 + '%1' + Title;

  Dependency.Filename := Filename;
  Dependency.Parameters := Parameters;
  Dependency.Title := Title;

  if FileExists(ExpandConstant('{tmp}{\}') + Filename) then begin
    Dependency.URL := '';
  end else begin
    Dependency.URL := URL;
  end;

  Dependency.Checksum := Checksum;
  Dependency.ForceSuccess := ForceSuccess;
  Dependency.RestartAfter := RestartAfter;

  DependencyCount := GetArrayLength(Dependency_List);
  SetArrayLength(Dependency_List, DependencyCount + 1);
  Dependency_List[DependencyCount] := Dependency;
end;

<event('InitializeWizard')>
procedure Dependency_Internal1;
begin
  Dependency_DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
end;

<event('PrepareToInstall')>
function Dependency_Internal2(var NeedsRestart: Boolean): String;
var
  DependencyCount, DependencyIndex, ResultCode: Integer;
  Retry: Boolean;
  TempValue: String;
begin
  DependencyCount := GetArrayLength(Dependency_List);

  if DependencyCount > 0 then begin
    Dependency_DownloadPage.Show;

    for DependencyIndex := 0 to DependencyCount - 1 do begin
      if Dependency_List[DependencyIndex].URL <> '' then begin
        Dependency_DownloadPage.Clear;
        Dependency_DownloadPage.Add(Dependency_List[DependencyIndex].URL, Dependency_List[DependencyIndex].Filename, Dependency_List[DependencyIndex].Checksum);

        Retry := True;
        while Retry do begin
          Retry := False;

          try
            Dependency_DownloadPage.Download;
          except
            if Dependency_DownloadPage.AbortedByUser then begin
              Result := Dependency_List[DependencyIndex].Title;
              DependencyIndex := DependencyCount;
            end else begin
              case SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbError, MB_ABORTRETRYIGNORE, IDIGNORE) of
                IDABORT: begin
                  Result := Dependency_List[DependencyIndex].Title;
                  DependencyIndex := DependencyCount;
                end;
                IDRETRY: begin
                  Retry := True;
                end;
              end;
            end;
          end;
        end;
      end;
    end;

    if Result = '' then begin
      for DependencyIndex := 0 to DependencyCount - 1 do begin
        Dependency_DownloadPage.SetText(Dependency_List[DependencyIndex].Title, '');
        Dependency_DownloadPage.SetProgress(DependencyIndex + 1, DependencyCount + 1);

        while True do begin
          ResultCode := 0;
          if ShellExec('', ExpandConstant('{tmp}{\}') + Dependency_List[DependencyIndex].Filename, Dependency_List[DependencyIndex].Parameters, '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then begin
            if Dependency_List[DependencyIndex].RestartAfter then begin
              if DependencyIndex = DependencyCount - 1 then begin
                Dependency_NeedRestart := True;
              end else begin
                NeedsRestart := True;
                Result := Dependency_List[DependencyIndex].Title;
              end;
              break;
            end else if (ResultCode = 0) or Dependency_List[DependencyIndex].ForceSuccess then begin // ERROR_SUCCESS (0)
              break;
            end else if ResultCode = 1641 then begin // ERROR_SUCCESS_REBOOT_INITIATED (1641)
              NeedsRestart := True;
              Result := Dependency_List[DependencyIndex].Title;
              break;
            end else if ResultCode = 3010 then begin // ERROR_SUCCESS_REBOOT_REQUIRED (3010)
              Dependency_NeedRestart := True;
              break;
            end;
          end;

          case SuppressibleMsgBox(FmtMessage(SetupMessage(msgErrorFunctionFailed), [Dependency_List[DependencyIndex].Title, IntToStr(ResultCode)]), mbError, MB_ABORTRETRYIGNORE, IDIGNORE) of
            IDABORT: begin
              Result := Dependency_List[DependencyIndex].Title;
              break;
            end;
            IDIGNORE: begin
              break;
            end;
          end;
        end;

        if Result <> '' then begin
          break;
        end;
      end;

      if NeedsRestart then begin
        TempValue := '"' + ExpandConstant('{srcexe}') + '" /restart=1 /LANG="' + ExpandConstant('{language}') + '" /DIR="' + WizardDirValue + '" /GROUP="' + WizardGroupValue + '" /TYPE="' + WizardSetupType(False) + '" /COMPONENTS="' + WizardSelectedComponents(False) + '" /TASKS="' + WizardSelectedTasks(False) + '"';
        if WizardNoIcons then begin
          TempValue := TempValue + ' /NOICONS';
        end;
        RegWriteStringValue(HKA, 'SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce', '{#SetupSetting("AppName")}', TempValue);
      end;
    end;

    Dependency_DownloadPage.Hide;
  end;
end;

<event('UpdateReadyMemo')>
function Dependency_Internal3(const Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
begin
  Result := '';
  if MemoUserInfoInfo <> '' then begin
    Result := Result + MemoUserInfoInfo + Newline + NewLine;
  end;
  if MemoDirInfo <> '' then begin
    Result := Result + MemoDirInfo + Newline + NewLine;
  end;
  if MemoTypeInfo <> '' then begin
    Result := Result + MemoTypeInfo + Newline + NewLine;
  end;
  if MemoComponentsInfo <> '' then begin
    Result := Result + MemoComponentsInfo + Newline + NewLine;
  end;
  if MemoGroupInfo <> '' then begin
    Result := Result + MemoGroupInfo + Newline + NewLine;
  end;
  if MemoTasksInfo <> '' then begin
    Result := Result + MemoTasksInfo;
  end;

  if Dependency_Memo <> '' then begin
    if MemoTasksInfo = '' then begin
      Result := Result + SetupMessage(msgReadyMemoTasks);
    end;
    Result := Result + FmtMessage(Dependency_Memo, [Space]);
  end;
end;

<event('NeedRestart')>
function Dependency_Internal4: Boolean;
begin
  Result := Dependency_NeedRestart;
end;

function Dependency_IsX64: Boolean;
begin
  Result := not Dependency_ForceX86 and Is64BitInstallMode;
end;

function Dependency_String(const x86, x64: String): String;
begin
  if Dependency_IsX64 then begin
    Result := x64;
  end else begin
    Result := x86;
  end;
end;

function Dependency_ArchSuffix: String;
begin
  Result := Dependency_String('', '_x64');
end;

function Dependency_ArchTitle: String;
begin
  Result := Dependency_String(' (x86)', ' (x64)');
end;

function Dependency_IsNetCoreInstalled(const Version: String): Boolean;
var
  ResultCode: Integer;
begin
  // source code: https://github.com/dotnet/deployment-tools/tree/master/src/clickonce/native/projects/NetCoreCheck
  if not FileExists(ExpandConstant('{tmp}{\}') + 'netcorecheck' + Dependency_ArchSuffix + '.exe') then begin
    ExtractTemporaryFile('netcorecheck' + Dependency_ArchSuffix + '.exe');
  end;
  Result := ShellExec('', ExpandConstant('{tmp}{\}') + 'netcorecheck' + Dependency_ArchSuffix + '.exe', Version, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

procedure Dependency_AddDotNet35;
begin
  // https://dotnet.microsoft.com/download/dotnet-framework/net35-sp1
  if not IsDotNetInstalled(net35, 1) then begin
    Dependency_Add('dotnetfx35.exe',
      '/lang:enu /passive /norestart',
      '.NET Framework 3.5 Service Pack 1',
      'https://download.microsoft.com/download/2/0/E/20E90413-712F-438C-988E-FDAA79A8AC3D/dotnetfx35.exe',
      '', False, False);
  end;
end;

procedure Dependency_AddDotNet40;
begin
  // https://dotnet.microsoft.com/download/dotnet-framework/net40
  if not IsDotNetInstalled(net4full, 0) then begin
    Dependency_Add('dotNetFx40_Full_setup.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Framework 4.0',
      'https://download.microsoft.com/download/1/B/E/1BE39E79-7E39-46A3-96FF-047F95396215/dotNetFx40_Full_setup.exe',
      '', False, False);
  end;
end;

procedure Dependency_AddDotNet45;
begin
  // https://dotnet.microsoft.com/download/dotnet-framework/net452
  if not IsDotNetInstalled(net452, 0) then begin
    Dependency_Add('dotnetfx45.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Framework 4.5.2',
      'https://go.microsoft.com/fwlink/?LinkId=397707',
      '', False, False);
  end;
end;

procedure Dependency_AddDotNet46;
begin
  // https://dotnet.microsoft.com/download/dotnet-framework/net462
  if not IsDotNetInstalled(net462, 0) then begin
    Dependency_Add('dotnetfx46.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Framework 4.6.2',
      'https://go.microsoft.com/fwlink/?linkid=780596',
      '', False, False);
  end;
end;

procedure Dependency_AddDotNet47;
begin
  // https://dotnet.microsoft.com/download/dotnet-framework/net472
  if not IsDotNetInstalled(net472, 0) then begin
    Dependency_Add('dotnetfx47.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Framework 4.7.2',
      'https://go.microsoft.com/fwlink/?LinkId=863262',
      '', False, False);
  end;
end;

procedure Dependency_AddDotNet48;
begin
  // https://dotnet.microsoft.com/download/dotnet-framework/net48
  if not IsDotNetInstalled(net48, 0) then begin
    Dependency_Add('dotnetfx48.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Framework 4.8',
      'https://go.microsoft.com/fwlink/?LinkId=2085155',
      '', False, False);
  end;
end;

procedure Dependency_AddNetCore31;
begin
  // https://dotnet.microsoft.com/download/dotnet-core/3.1
  if not Dependency_IsNetCoreInstalled('Microsoft.NETCore.App 3.1.22') then begin
    Dependency_Add('netcore31' + Dependency_ArchSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Core Runtime 3.1.22' + Dependency_ArchTitle,
      Dependency_String('https://download.visualstudio.microsoft.com/download/pr/c2437aed-8cc4-41d0-a239-d6c7cf7bddae/062c37e8b06df740301c0bca1b0b7b9a/dotnet-runtime-3.1.22-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/4e95705e-1bb6-4764-b899-1b97eb70ea1d/dd311e073bd3e25b2efe2dcf02727e81/dotnet-runtime-3.1.22-win-x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddNetCore31Asp;
begin
  // https://dotnet.microsoft.com/download/dotnet-core/3.1
  if not Dependency_IsNetCoreInstalled('Microsoft.AspNetCore.App 3.1.22') then begin
    Dependency_Add('netcore31asp' + Dependency_ArchSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      'ASP.NET Core Runtime 3.1.22' + Dependency_ArchTitle,
      Dependency_String('https://download.visualstudio.microsoft.com/download/pr/0a1a2ee5-b8ed-4f0d-a4af-a7bce9a9ac2b/d452039b49d79e8897f272c3ab34b875/aspnetcore-runtime-3.1.22-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/80e52143-31e8-450e-aa94-b3f8484aaba9/4b69e5c77d50e7b367960a0079c90a99/aspnetcore-runtime-3.1.22-win-x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddNetCore31Desktop;
begin
  // https://dotnet.microsoft.com/download/dotnet-core/3.1
  if not Dependency_IsNetCoreInstalled('Microsoft.WindowsDesktop.App 3.1.22') then begin
    Dependency_Add('netcore31desktop' + Dependency_ArchSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Desktop Runtime 3.1.22' + Dependency_ArchTitle,
      Dependency_String('https://download.visualstudio.microsoft.com/download/pr/e4fcd574-4487-4b4b-8ca8-c23177c6f59f/c6d67a04956169dc21895cdcb42bf344/windowsdesktop-runtime-3.1.22-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/1c14e24b-7f31-42dc-ba3c-83295a2d6f7e/41b93591162dfe556cc160ae44fbe75e/windowsdesktop-runtime-3.1.22-win-x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddDotNet50;
begin
  // https://dotnet.microsoft.com/download/dotnet/5.0
  if not Dependency_IsNetCoreInstalled('Microsoft.NETCore.App 5.0.13') then begin
    Dependency_Add('dotnet50' + Dependency_ArchSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Runtime 5.0.13' + Dependency_ArchTitle,
      Dependency_String('https://download.visualstudio.microsoft.com/download/pr/4a79fcd5-d61b-4606-8496-68071c8099c6/2bf770ca40521e8c4563072592eadd06/dotnet-runtime-5.0.13-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/fccf43d2-3e62-4ede-b5a5-592a7ccded7b/6339f1fdfe3317df5b09adf65f0261ab/dotnet-runtime-5.0.13-win-x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddDotNet50Asp;
begin
  // https://dotnet.microsoft.com/download/dotnet/5.0
  if not Dependency_IsNetCoreInstalled('Microsoft.AspNetCore.App 5.0.13') then begin
    Dependency_Add('dotnet50asp' + Dependency_ArchSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      'ASP.NET Core Runtime 5.0.13' + Dependency_ArchTitle,
      Dependency_String('https://download.visualstudio.microsoft.com/download/pr/340f9482-fc43-4ef7-b434-e2ed57f55cb3/c641b805cef3823769409a6dbac5746b/aspnetcore-runtime-5.0.13-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/aac560f3-eac8-437e-aebd-9830119deb10/6a3880161cf527e4ec71f67efe4d91ad/aspnetcore-runtime-5.0.13-win-x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddDotNet50Desktop;
begin
  // https://dotnet.microsoft.com/download/dotnet/5.0
  if not Dependency_IsNetCoreInstalled('Microsoft.WindowsDesktop.App 5.0.13') then begin
    Dependency_Add('dotnet50desktop' + Dependency_ArchSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Desktop Runtime 5.0.13' + Dependency_ArchTitle,
      Dependency_String('https://download.visualstudio.microsoft.com/download/pr/c8125c6b-d399-4be3-b201-8f1394fc3b25/724758f754fc7b67daba74db8d6d91d9/windowsdesktop-runtime-5.0.13-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/2bfb80f2-b8f2-44b0-90c1-d3c8c1c8eac8/409dd3d3367feeeda048f4ff34b32e82/windowsdesktop-runtime-5.0.13-win-x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddDotNet60;
begin
  // https://dotnet.microsoft.com/download/dotnet/6.0
  if not Dependency_IsNetCoreInstalled('Microsoft.NETCore.App 6.0.8') then begin
    Dependency_Add('dotnet60' + Dependency_ArchSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Runtime 6.0.8' + Dependency_ArchTitle,
      Dependency_String('https://download.visualstudio.microsoft.com/download/pr/db70cd1d-4f33-4dc4-8293-57bb362175c7/5c27048a0fc814e58bc196666a9b0c61/dotnet-runtime-6.0.8-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/5af3de9d-1e5f-48ff-bfb7-f93c0957ffae/e8dd664b0439f4725f8c968e7aae7dd1/dotnet-runtime-6.0.8-win-x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddDotNet60Asp;
begin
  // https://dotnet.microsoft.com/download/dotnet/6.0
  if not Dependency_IsNetCoreInstalled('Microsoft.AspNetCore.App 6.0.8') then begin
    Dependency_Add('dotnet60asp' + Dependency_ArchSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      'ASP.NET Core Runtime 6.0.8' + Dependency_ArchTitle,
      Dependency_String('https://download.visualstudio.microsoft.com/download/pr/26dd0df5-f2ef-4b47-8651-84a2496dd017/158f3a45dd0718fc3ceda10b56b22721/aspnetcore-runtime-6.0.8-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/f5ef50c0-4dd1-4301-857f-768834d5a006/852c6470e4e5f602eee280c1e4e4e4c3/aspnetcore-runtime-6.0.8-win-x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddDotNet60Desktop;
begin
  // https://dotnet.microsoft.com/download/dotnet/6.0
  if not Dependency_IsNetCoreInstalled('Microsoft.WindowsDesktop.App 6.0.8') then begin
    Dependency_Add('dotnet60desktop' + Dependency_ArchSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Desktop Runtime 6.0.8' + Dependency_ArchTitle,
      Dependency_String('https://download.visualstudio.microsoft.com/download/pr/61747fc6-7236-4d5e-85e5-a5df5f480f3a/02203594bf1331f0875aa6491419ffa1/windowsdesktop-runtime-6.0.8-win-x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/b4a17a47-2fe8-498d-b817-30ad2e23f413/00020402af25ba40990c6cc3db5cb270/windowsdesktop-runtime-6.0.8-win-x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddVC2005;
begin
  // https://www.microsoft.com/en-us/download/details.aspx?id=26347
  if not IsMsiProductInstalled(Dependency_String('{86C9D5AA-F00C-4921-B3F2-C60AF92E2844}', '{A8D19029-8E5C-4E22-8011-48070F9E796E}'), PackVersionComponents(8, 0, 61000, 0)) then begin
    Dependency_Add('vcredist2005' + Dependency_ArchSuffix + '.exe',
      '/q',
      'Visual C++ 2005 Service Pack 1 Redistributable' + Dependency_ArchTitle,
      Dependency_String('https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x86.EXE', 'https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x64.EXE'),
      '', False, False);
  end;
end;

procedure Dependency_AddVC2008;
begin
  // https://www.microsoft.com/en-us/download/details.aspx?id=26368
  if not IsMsiProductInstalled(Dependency_String('{DE2C306F-A067-38EF-B86C-03DE4B0312F9}', '{FDA45DDF-8E17-336F-A3ED-356B7B7C688A}'), PackVersionComponents(9, 0, 30729, 6161)) then begin
    Dependency_Add('vcredist2008' + Dependency_ArchSuffix + '.exe',
      '/q',
      'Visual C++ 2008 Service Pack 1 Redistributable' + Dependency_ArchTitle,
      Dependency_String('https://download.microsoft.com/download/5/D/8/5D8C65CB-C849-4025-8E95-C3966CAFD8AE/vcredist_x86.exe', 'https://download.microsoft.com/download/5/D/8/5D8C65CB-C849-4025-8E95-C3966CAFD8AE/vcredist_x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddVC2010;
begin
  // https://www.microsoft.com/en-us/download/details.aspx?id=26999
  if not IsMsiProductInstalled(Dependency_String('{1F4F1D2A-D9DA-32CF-9909-48485DA06DD5}', '{5B75F761-BAC8-33BC-A381-464DDDD813A3}'), PackVersionComponents(10, 0, 40219, 0)) then begin
    Dependency_Add('vcredist2010' + Dependency_ArchSuffix + '.exe',
      '/passive /norestart',
      'Visual C++ 2010 Service Pack 1 Redistributable' + Dependency_ArchTitle,
      Dependency_String('https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe', 'https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddVC2012;
begin
  // https://www.microsoft.com/en-us/download/details.aspx?id=30679
  if not IsMsiProductInstalled(Dependency_String('{4121ED58-4BD9-3E7B-A8B5-9F8BAAE045B7}', '{EFA6AFA1-738E-3E00-8101-FD03B86B29D1}'), PackVersionComponents(11, 0, 61030, 0)) then begin
    Dependency_Add('vcredist2012' + Dependency_ArchSuffix + '.exe',
      '/passive /norestart',
      'Visual C++ 2012 Update 4 Redistributable' + Dependency_ArchTitle,
      Dependency_String('https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x86.exe', 'https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddVC2013;
begin
  // https://support.microsoft.com/en-us/help/4032938
  if not IsMsiProductInstalled(Dependency_String('{B59F5BF1-67C8-3802-8E59-2CE551A39FC5}', '{20400CF0-DE7C-327E-9AE4-F0F38D9085F8}'), PackVersionComponents(12, 0, 40664, 0)) then begin
    Dependency_Add('vcredist2013' + Dependency_ArchSuffix + '.exe',
      '/passive /norestart',
      'Visual C++ 2013 Update 5 Redistributable' + Dependency_ArchTitle,
      Dependency_String('https://download.visualstudio.microsoft.com/download/pr/10912113/5da66ddebb0ad32ebd4b922fd82e8e25/vcredist_x86.exe', 'https://download.visualstudio.microsoft.com/download/pr/10912041/cee5d6bca2ddbcd039da727bf4acb48a/vcredist_x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddVC2015To2022;
begin
  // https://docs.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist
  if not IsMsiProductInstalled(Dependency_String('{65E5BD06-6392-3027-8C26-853107D3CF1A}', '{36F68A90-239C-34DF-B58C-64B30153CE35}'), PackVersionComponents(14, 30, 30704, 0)) then begin
    Dependency_Add('vcredist2022' + Dependency_ArchSuffix + '.exe',
      '/passive /norestart',
      'Visual C++ 2015-2022 Redistributable' + Dependency_ArchTitle,
      Dependency_String('https://aka.ms/vs/17/release/vc_redist.x86.exe', 'https://aka.ms/vs/17/release/vc_redist.x64.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddDirectX;
begin
  // https://www.microsoft.com/en-us/download/details.aspx?id=35
  Dependency_Add('dxwebsetup.exe',
    '/q',
    'DirectX Runtime',
    'https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E18C4C/dxwebsetup.exe',
    '', True, False);
end;

procedure Dependency_AddSql2008Express;
var
  Version: String;
  PackedVersion: Int64;
begin
  // https://www.microsoft.com/en-us/download/details.aspx?id=30438
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL10_50.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(10, 50, 4000, 0)) < 0) then begin
    Dependency_Add('sql2008express' + Dependency_ArchSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2008 R2 Service Pack 2 Express',
      Dependency_String('https://download.microsoft.com/download/0/4/B/04BE03CD-EAF3-4797-9D8D-2E08E316C998/SQLEXPR32_x86_ENU.exe', 'https://download.microsoft.com/download/0/4/B/04BE03CD-EAF3-4797-9D8D-2E08E316C998/SQLEXPR_x64_ENU.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddSql2012Express;
var
  Version: String;
  PackedVersion: Int64;
begin
  // https://www.microsoft.com/en-us/download/details.aspx?id=56042
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL11.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(11, 0, 7001, 0)) < 0) then begin
    Dependency_Add('sql2012express' + Dependency_ArchSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2012 Service Pack 4 Express',
      Dependency_String('https://download.microsoft.com/download/B/D/E/BDE8FAD6-33E5-44F6-B714-348F73E602B6/SQLEXPR32_x86_ENU.exe', 'https://download.microsoft.com/download/B/D/E/BDE8FAD6-33E5-44F6-B714-348F73E602B6/SQLEXPR_x64_ENU.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddSql2014Express;
var
  Version: String;
  PackedVersion: Int64;
begin
  // https://www.microsoft.com/en-us/download/details.aspx?id=57473
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL12.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(12, 0, 6024, 0)) < 0) then begin
    Dependency_Add('sql2014express' + Dependency_ArchSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2014 Service Pack 3 Express',
      Dependency_String('https://download.microsoft.com/download/3/9/F/39F968FA-DEBB-4960-8F9E-0E7BB3035959/SQLEXPR32_x86_ENU.exe', 'https://download.microsoft.com/download/3/9/F/39F968FA-DEBB-4960-8F9E-0E7BB3035959/SQLEXPR_x64_ENU.exe'),
      '', False, False);
  end;
end;

procedure Dependency_AddSql2016Express;
var
  Version: String;
  PackedVersion: Int64;
begin
  // https://www.microsoft.com/en-us/download/details.aspx?id=56840
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL13.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(13, 0, 5026, 0)) < 0) then begin
    Dependency_Add('sql2016express' + Dependency_ArchSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2016 Service Pack 2 Express',
      'https://download.microsoft.com/download/3/7/6/3767D272-76A1-4F31-8849-260BD37924E4/SQLServer2016-SSEI-Expr.exe',
      '', False, False);
  end;
end;

procedure Dependency_AddSql2017Express;
var
  Version: String;
  PackedVersion: Int64;
begin
  // https://www.microsoft.com/en-us/download/details.aspx?id=55994
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL14.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(14, 0, 0, 0)) < 0) then begin
    Dependency_Add('sql2017express' + Dependency_ArchSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2017 Express',
      'https://download.microsoft.com/download/5/E/9/5E9B18CC-8FD5-467E-B5BF-BADE39C51F73/SQLServer2017-SSEI-Expr.exe',
      '', False, False);
  end;
end;

procedure Dependency_AddSql2019Express;
var
  Version: String;
  PackedVersion: Int64;
begin
  // https://www.microsoft.com/en-us/download/details.aspx?id=101064
  if not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQLServer\CurrentVersion', 'CurrentVersion', Version) or not StrToVersion(Version, PackedVersion) or (ComparePackedVersion(PackedVersion, PackVersionComponents(15, 0, 0, 0)) < 0) then begin
    Dependency_Add('sql2019express' + Dependency_ArchSuffix + '.exe',
      '/QS /IACCEPTSQLSERVERLICENSETERMS /ACTION=INSTALL /FEATURES=SQL /INSTANCENAME=MSSQLSERVER',
      'SQL Server 2019 Express',
      'https://download.microsoft.com/download/7/f/8/7f8a9c43-8c8a-4f7c-9f92-83c18d96b681/SQL2019-SSEI-Expr.exe',
      '', False, False);
  end;
end;

procedure Dependency_AddWebView2;
begin
  if not RegValueExists(HKLM, Dependency_String('SOFTWARE', 'SOFTWARE\WOW6432Node') + '\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv') then begin
    Dependency_Add('MicrosoftEdgeWebview2Setup.exe',
      '/silent /install',
      'WebView2 Runtime',
      'https://go.microsoft.com/fwlink/p/?LinkId=2124703',
      '', False, False);
  end;
end;


[Setup]
; -------------
; EXAMPLE SETUP
; -------------

; comment out dependency defines to disable installing them
;#define UseDotNet35
;#define UseDotNet40
;#define UseDotNet45
;#define UseDotNet46
;#define UseDotNet47
#define UseDotNet48

; requires netcorecheck.exe and netcorecheck_x64.exe (see download link below)
;#define UseNetCoreCheck
;#ifdef UseNetCoreCheck
;  #define UseNetCore31
;  #define UseNetCore31Asp
;  #define UseNetCore31Desktop
;  #define UseDotNet50
;  #define UseDotNet50Asp
;  #define UseDotNet50Desktop
;  #define UseDotNet60
;  #define UseDotNet60Asp
;  #define UseDotNet60Desktop
;#endif

;#define UseVC2005
;#define UseVC2008
;#define UseVC2010
;#define UseVC2012
;#define UseVC2013
;#define UseVC2015To2022

; requires dxwebsetup.exe (see download link below)
;#define UseDirectX

;#define UseSql2008Express
;#define UseSql2012Express
;#define UseSql2014Express
;#define UseSql2016Express
;#define UseSql2017Express
;#define UseSql2019Express

;#define UseWebView2

#define MyAppSetupName 'Purple Pen'
#define MyAppName "Purple Pen"
#define MyAppVersion "3.5.0.500"
#define MyAppPublisher "Golde Software"
#define MyAppURL "http://purple-pen.org"
#define MyAppExeName "PurplePen.exe"
#define BuildDir "..\PurplePen\bin\Release"

; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{347D1E62-7134-4827-9679-4952BEC91C95}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
UninstallDisplayName={#MyAppName}
DefaultDirName={commonpf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=output
OutputBaseFilename=purplepen-setup
Compression=lzma
SolidCompression=yes
ChangesAssociations=yes

DisableProgramGroupPage=yes
ShowLanguageDialog=auto

;SignTool=signhelper
;SignedUninstaller=yes

UninstallDisplayIcon={app}\{#MyAppExeName},0

;MinVersion default value: "0,5.0 (Windows 2000+) if Unicode Inno Setup, else 4.0,4.0 (Windows 95+)"

; Min version: Windows 7 SP1
MinVersion=6.1sp1
PrivilegesRequired=admin

;These were turned on in the sample for the bootstrapper, but I turned them off again.
;ArchitecturesAllowed=x86 x64 ia64
;ArchitecturesInstallIn64BitMode=x64 ia64

; downloading and installing dependencies will only work if the memo/ready page is enabled (default and current behaviour)
DisableReadyPage=no
DisableReadyMemo=no

; supported languages
#include "scripts\lang\english.iss"
#include "scripts\lang\german.iss"
#include "scripts\lang\french.iss"
#include "scripts\lang\italian.iss"
#include "scripts\lang\dutch.iss"

#ifdef UNICODE
#include "scripts\lang\chinese.iss"
#include "scripts\lang\polish.iss"
#include "scripts\lang\russian.iss"
#include "scripts\lang\japanese.iss"
#endif


[Files]
#ifdef UseNetCoreCheck
; download netcorecheck.exe: https://go.microsoft.com/fwlink/?linkid=2135256
; download netcorecheck_x64.exe: https://go.microsoft.com/fwlink/?linkid=2135504
Source: "netcorecheck.exe"; Flags: dontcopy noencryption
Source: "netcorecheck_x64.exe"; Flags: dontcopy noencryption
#endif

#ifdef UseDirectX
Source: "dxwebsetup.exe"; Flags: dontcopy noencryption
#endif

Source: "{#BuildDir}\PurplePen.exe"; DestDir: "{app}"; Flags: ignoreversion 
Source: "{#BuildDir}\CrashReporter.NET.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "{#BuildDir}\DotSpatial.Projections.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "{#BuildDir}\GDIPlusNative.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "{#BuildDir}\GDIPlusNative64.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "{#BuildDir}\Graphics2D.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "{#BuildDir}\ICSharpCode.SharpZipLib.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Map_GDIPlus.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "{#BuildDir}\Map_PDF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Map_WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\MapModel.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\PdfConverter.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\..\..\..\PdfConverter\bin\Release\PdfConverter.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\PdfiumViewer.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\PdfSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Purple Pen Help.chm"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\PurplePen.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\symbols.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\USWebCoatedSWOP.icc"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Samples\*"; DestDir: "{app}\Samples"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\x64\*"; DestDir: "{app}\x64"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\x86\*"; DestDir: "{app}\x86"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\bg\*"; DestDir: "{app}\bg"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\cs\*"; DestDir: "{app}\cs"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\da\*"; DestDir: "{app}\da"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\de\*"; DestDir: "{app}\de"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\es\*"; DestDir: "{app}\es"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\et\*"; DestDir: "{app}\et"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\fi\*"; DestDir: "{app}\fi"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\fr\*"; DestDir: "{app}\fr"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\hu\*"; DestDir: "{app}\hu"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\ja\*"; DestDir: "{app}\ja"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\nb-NO\*"; DestDir: "{app}\nb-NO"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\nl\*"; DestDir: "{app}\nl"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\nn-NO\*"; DestDir: "{app}\nn-NO"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\pl\*"; DestDir: "{app}\pl"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\pt-BR\*"; DestDir: "{app}\pt-BR"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\pt\*"; DestDir: "{app}\pt"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\ro\*"; DestDir: "{app}\ro"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\sv\*"; DestDir: "{app}\sv"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\tr\*"; DestDir: "{app}\tr"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\uk\*"; DestDir: "{app}\uk"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\zh-CN\*"; DestDir: "{app}\zh-CN"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#BuildDir}\zh-TW\*"; DestDir: "{app}\zh-TW"; Flags: ignoreversion recursesubdirs createallsubdirs

Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-Regular.ttf"; DestDir: "{app}\fonts"; Flags: ignoreversion recursesubdirs createallsubdirs 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-Bold.ttf"; DestDir: "{app}\fonts"; Flags: ignoreversion recursesubdirs createallsubdirs 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-Italic.ttf"; DestDir: "{app}\fonts"; Flags: ignoreversion recursesubdirs createallsubdirs 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-BoldItalic.ttf"; DestDir: "{app}\fonts"; Flags: ignoreversion recursesubdirs createallsubdirs 
Source: "{#BuildDir}\..\..\..\RobotoFont\RobotoCondensed-Regular.ttf"; DestDir: "{app}\fonts"; Flags: ignoreversion recursesubdirs createallsubdirs 
Source: "{#BuildDir}\..\..\..\RobotoFont\RobotoCondensed-Bold.ttf"; DestDir: "{app}\fonts"; Flags: ignoreversion recursesubdirs createallsubdirs 
Source: "{#BuildDir}\..\..\..\RobotoFont\RobotoCondensed-Italic.ttf"; DestDir: "{app}\fonts"; Flags: ignoreversion recursesubdirs createallsubdirs 
Source: "{#BuildDir}\..\..\..\RobotoFont\RobotoCondensed-BoldItalic.ttf"; DestDir: "{app}\fonts"; Flags: ignoreversion recursesubdirs createallsubdirs 

Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-Regular.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-Bold.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Bold"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-Italic.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Italic"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-BoldItalic.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Bold Italic"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-Black.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Black"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-BlackItalic.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Black Italic"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-Light.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Light"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-LightItalic.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Light Italic"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-Medium.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Medium"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-MediumItalic.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Medium Italic"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-Thin.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Thin"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\Roboto-ThinItalic.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Thin Italic"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\RobotoCondensed-Regular.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Condensed"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\RobotoCondensed-Bold.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Condensed Bold"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\RobotoCondensed-Italic.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Condensed Italic"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\RobotoCondensed-BoldItalic.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Condensed Bold Italic"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\RobotoCondensed-Light.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Condensed Light"; Flags: onlyifdoesntexist uninsneveruninstall 
Source: "{#BuildDir}\..\..\..\RobotoFont\RobotoCondensed-LightItalic.ttf"; DestDir: "{commonfonts}"; FontInstall: "Roboto Condensed Light Italic"; Flags: onlyifdoesntexist uninsneveruninstall 

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{commonprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCR; SubKey: ".ppen"; ValueType: string; ValueData: "Purple Pen Files"; Flags: uninsdeletekey
Root: HKCR; SubKey: "Purple Pen Files"; ValueType: string; ValueData: "Purple Pen Event File"; Flags: uninsdeletekey
Root: HKCR; SubKey: "Purple Pen Files\Shell\Open\Command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey
Root: HKCR; Subkey: "Purple Pen Files\DefaultIcon"; ValueType: string; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletevalue

[CustomMessages]
DependenciesDir=MyProgramDependencies
WindowsServicePack=Windows %1 Service Pack %2

[Code]

const
  // This is a list of all the old InstallShield app ids that we should uninstall
  // before installation.
  OldAppIds = '{3B58B548-0ECD-4ADA-93F0-CD2391051E36},{429A824E-8BC4-4317-B1E1-6CB5C4D764EE},{E788526D-59B0-4CBC-BC5D-F064E7A01A7F},{95A47628-872C-4BF9-A108-4204113A6FC6},{FCD4408C-26FE-4C88-A008-95FCD5EC9079},' +
              '{F8BF1EAA-844A-4B13-AD00-62400084CF0F},{64352D23-29F2-4AAC-A18D-0D8583BE91CC},{CFCECA27-0BEA-49B0-A0B1-269D33FED29A},{8E49D865-B9CB-46AB-BC89-CCEE165ED9DB},{7B807BE7-506F-404F-95CB-BDBB00535010},' +
			  '{3A0A803D-20BC-4137-AD76-C242AEED29AC},{AAC50A38-6ED4-4702-89FF-E2B1FBE459F2},{02CC5ED8-227C-4533-AB89-14E8D0C0EF5B},{66C9FB5B-61B3-4B34-BB99-9F12BC9E89E0},{4326052A-5A34-4B9C-88CF-83FF56182B41},' +
			  '{CC58D288-8B02-442B-97BC-538FD9E91350},{4FA6D9DF-9AAE-4107-9788-FE50B568F0E7},{5BDA646D-D28B-421D-AB9B-5A665B155857},{73B952E2-83A5-4EF3-B7C0-021F8434ED12},{BFA9EBB2-EFB5-4383-8048-1515FB21E3BF},' +
			  '{2C5471C8-2725-40FF-8BA3-DA231E2E97E3},{79D33ADC-3851-43D0-8D32-C6C050DAEFD1},{DEEA0FF7-76F7-436A-9291-532CF60F1C71},{E5210D4C-7DDA-4129-B093-4E9FDC3CC5DA},{0A95E799-4F85-4D04-8CFE-C9F97BAA7CCF},' +
			  '{0F25C741-8C36-4900-BB0B-808916F3950E},{E0C667D6-1959-4F4F-A96C-4D6F547E9102},{28A0421F-72A9-45ED-8B83-7B900BD4F825},{36FFCD63-8922-4DB6-92B1-6734268F6DA7},{74E3ACC5-8326-402C-B793-6B698DEF74F2},' +
			  '{9C4992FA-DF21-488B-A6DD-48EC0EF94686},{77B1343B-A564-4F73-824B-FB60B7B3AD42},{9E76C620-36D2-4452-9AEA-F96BD598E794},{EFCB7AC9-09D9-43E2-9A97-76DE338A208C},{6B5026C8-36D4-4D80-BEFD-4836761BDD24}';



function InitializeSetup: Boolean;
begin
#ifdef UseDotNet35
  Dependency_AddDotNet35;
#endif
#ifdef UseDotNet40
  Dependency_AddDotNet40;
#endif
#ifdef UseDotNet45
  Dependency_AddDotNet45;
#endif
#ifdef UseDotNet46
  Dependency_AddDotNet46;
#endif
#ifdef UseDotNet47
  Dependency_AddDotNet47;
#endif
#ifdef UseDotNet48
  Dependency_AddDotNet48;
#endif

#ifdef UseNetCore31
  Dependency_AddNetCore31;
#endif
#ifdef UseNetCore31Asp
  Dependency_AddNetCore31Asp;
#endif
#ifdef UseNetCore31Desktop
  Dependency_AddNetCore31Desktop;
#endif
#ifdef UseDotNet50
  Dependency_AddDotNet50;
#endif
#ifdef UseDotNet50Asp
  Dependency_AddDotNet50Asp;
#endif
#ifdef UseDotNet50Desktop
  Dependency_AddDotNet50Desktop;
#endif
#ifdef UseDotNet60
  Dependency_AddDotNet60;
#endif
#ifdef UseDotNet60Asp
  Dependency_AddDotNet60Asp;
#endif
#ifdef UseDotNet60Desktop
  Dependency_AddDotNet60Desktop;
#endif

#ifdef UseVC2005
  Dependency_AddVC2005;
#endif
#ifdef UseVC2008
  Dependency_AddVC2008;
#endif
#ifdef UseVC2010
  Dependency_AddVC2010;
#endif
#ifdef UseVC2012
  Dependency_AddVC2012;
#endif
#ifdef UseVC2013
  //Dependency_ForceX86 := True; // force 32-bit install of next dependencies
  Dependency_AddVC2013;
  //Dependency_ForceX86 := False; // disable forced 32-bit install again
#endif
#ifdef UseVC2015To2022
  Dependency_AddVC2015To2022;
#endif

#ifdef UseDirectX
  ExtractTemporaryFile('dxwebsetup.exe');
  Dependency_AddDirectX;
#endif

#ifdef UseSql2008Express
  Dependency_AddSql2008Express;
#endif
#ifdef UseSql2012Express
  Dependency_AddSql2012Express;
#endif
#ifdef UseSql2014Express
  Dependency_AddSql2014Express;
#endif
#ifdef UseSql2016Express
  Dependency_AddSql2016Express;
#endif
#ifdef UseSql2017Express
  Dependency_AddSql2017Express;
#endif
#ifdef UseSql2019Express
  Dependency_AddSql2019Express;
#endif

#ifdef UseWebView2
  Dependency_AddWebView2;
#endif

  Result := True;
end;

/////////////////////////////////////////////////////////////////////
function GetUninstallString(appId: String): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + appId;
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;


/////////////////////////////////////////////////////////////////////
function IsUpgrade(appId: String): Boolean;
begin
  Result := (GetUninstallString(appId) <> '');
end;


/////////////////////////////////////////////////////////////////////
function UnInstallOldVersion(appId: String): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
// Return Values:
// 1 - uninstall string is empty
// 2 - error executing the UnInstallString
// 3 - successfully executed the UnInstallString

  // default return value
  Result := 0;

  // get the uninstall string of the old app
  sUnInstallString := GetUninstallString(appId);
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec('>', sUnInstallString + ' /qn','', SW_SHOW, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

/////////////////////////////////////////////////////////////////////
procedure CurStepChanged(CurStep: TSetupStep);
var
  i: Integer;
  appIdList: TStringList;
begin
  if (CurStep=ssInstall) then
  begin
    appIdList := TStringList.Create;
    appIdList.CommaText := OldAppIds;

    for i := 0 to appIdList.Count - 1 do
    begin
        UnInstallOldVersion(appIdList[i]);
    end;
  end;
end;