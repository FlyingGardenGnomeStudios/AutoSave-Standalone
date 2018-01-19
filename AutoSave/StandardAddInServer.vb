Imports Inventor
Imports System.Runtime.InteropServices
Imports Microsoft.Win32
Imports System.Linq
Imports System.Windows.Forms
Imports System.IO
Imports System.Collections.Generic
Imports Autodesk.WebServices
Imports System.Text
Imports RestSharp
Imports System.Net
Imports RestSharp.Deserializers


Namespace AutoSave
    <ProgIdAttribute("AutoSave.StandardAddInServer"),
    GuidAttribute("e3bdfb3b-6cc6-45cc-a0cf-90eeea53f9e2")>
    Public Class StandardAddInServer
        Implements Inventor.ApplicationAddInServer
        Dim WithEvents m_UIEvents2 As UserInputEvents
        Dim WithEvents m_UIEvents3 As InteractionEvents
        Private WithEvents m_uiEvents As UserInterfaceEvents
        Private WithEvents m_AutoSaveButton As ButtonDefinition
        Dim WithEvents m_AppEvents As ApplicationEvents
        Dim bkgAutoSave As System.Threading.Thread
        Dim _invApp As Inventor.Application
        Dim SkipSave As Boolean

#Region "ApplicationAddInServer Members"

        ' This method is called by Inventor when it loads the AddIn. The AddInSiteObject provides access  
        ' to the Inventor Application object. The FirstTime flag indicates if the AddIn is loaded for
        ' the first time. However, with the introduction of the ribbon this argument is always true.
        Public Sub Activate(ByVal addInSiteObject As Inventor.ApplicationAddInSite, ByVal firstTime As Boolean) Implements Inventor.ApplicationAddInServer.Activate
            ' Initialize AddIn members.
            g_inventorApplication = addInSiteObject.Application
            ' Connect to the user-interface events to handle a ribbon reset.
            m_uiEvents = g_inventorApplication.UserInterfaceManager.UserInterfaceEvents
            m_UIEvents2 = g_inventorApplication.CommandManager.UserInputEvents
            Dim largeIcon As stdole.IPictureDisp = PictureDispConverter.ToIPictureDisp(My.Resources.AutoSave_32)
            Dim smallIcon As stdole.IPictureDisp = PictureDispConverter.ToIPictureDisp(My.Resources.AutoSave_16)
            Dim controlDefs As Inventor.ControlDefinitions = g_inventorApplication.CommandManager.ControlDefinitions
            m_AutoSaveButton = controlDefs.AddButtonDefinition("Settings", "UIAutoSave", CommandTypesEnum.kShapeEditCmdType, AddInClientID,, "Change options for AutoSave", smallIcon, largeIcon, ButtonDisplayEnum.kDisplayTextInLearningMode)
            m_AppEvents = g_inventorApplication.ApplicationEvents
            ' Add to the user interface, if it's the first time.
            If firstTime Then
                AddToUserInterface()
            End If
        End Sub

        ' This method is called by Inventor when the AddIn is unloaded. The AddIn will be
        ' unloaded either manually by the user or when the Inventor session is terminated.
        Public Sub Deactivate() Implements Inventor.ApplicationAddInServer.Deactivate
            ' TODO:  Add ApplicationAddInServer.Deactivate implementation
            ' Release objects.
            m_uiEvents = Nothing
            g_inventorApplication = Nothing
            m_AppEvents = Nothing
            GC.Collect()
            GC.WaitForPendingFinalizers()
        End Sub

        ' This property is provided to allow the AddIn to expose an API of its own to other 
        ' programs. Typically, this  would be done by implementing the AddIn's API
        ' interface in a class and returning that class object through this property.
        Public ReadOnly Property Automation() As Object Implements Inventor.ApplicationAddInServer.Automation
            Get
                Return Nothing
            End Get
        End Property

        ' Note:this method is now obsolete, you should use the 
        ' ControlDefinition functionality for implementing commands.
        Public Sub ExecuteCommand(ByVal commandID As Integer) Implements Inventor.ApplicationAddInServer.ExecuteCommand
        End Sub
#End Region

#Region "AppStore Authentication"
        Private Function Entitlement(ByVal appId As String, ByVal userId As String) As Boolean
            'REST API call for the entitlement API.
            'We are using RestSharp for simplicity.
            'You may choose to use another library.
            '(1) Build request
            Dim client = New RestClient
            client.BaseUrl = New System.Uri("https://apps.exchange.autodesk.com")
            'Set resource/end point
            Dim request = New RestRequest
            request.Resource = "webservices/checkentitlement"
            request.Method = Method.GET
            'Add parameters
            request.AddParameter("userid", userId)
            request.AddParameter("appid", appId)
            ' (2) Execute request and get response
            Dim response As IRestResponse = client.Execute(request)
            ' (3) Parse the response and get the value of IsValid. 
            Dim isValid As Boolean = False
            If (response.StatusCode = HttpStatusCode.OK) Then
                Dim deserial As JsonDeserializer = New JsonDeserializer
                Dim entitlementResponse As EntitlementResponse = deserial.Deserialize(Of EntitlementResponse)(response)
                isValid = entitlementResponse.IsValid
            End If

            Return isValid
        End Function
#End Region
#Region "User interface definition"
        ' Sub where the user-interface creation is done.  This is called when
        ' the add-in loaded and also if the user interface is reset.
        Private Sub AddToUserInterface()
            '' Get the part ribbon.

            For Each Ribbon As Inventor.Ribbon In g_inventorApplication.UserInterfaceManager.Ribbons
                Dim GetStartedRibbon As Ribbon = g_inventorApplication.UserInterfaceManager.Ribbons.Item(Ribbon.InternalName)
                Dim GetStarted As RibbonTab = GetStartedRibbon.RibbonTabs.Item("id_GetStarted")
                '' Create a new panel.
                Dim AutoSave As RibbonPanel = GetStarted.RibbonPanels.Add("AutoSave", "AutoSave", AddInClientID)
                '' Add a button.
                AutoSave.CommandControls.AddButton(m_AutoSaveButton, True)
            Next
            Dim Reg As RegistryKey = My.Computer.Registry.CurrentUser.OpenSubKey("Software\Autodesk\Inventor\Current Version\AutoSave", True)
            If Not Reg Is Nothing Then
                bkgAutoSave = New System.Threading.Thread(AddressOf runAutoSave)
                bkgAutoSave.Start(Reg)
            End If
        End Sub

        Private Sub m_uiEvents_OnResetRibbonInterface(Context As NameValueMap) Handles m_uiEvents.OnResetRibbonInterface
            ' The ribbon was reset, so add back the add-ins user-interface.
            AddToUserInterface()
        End Sub
        ' Sample handler for the button.
        Private Sub m_AutoSaveButton_OnExecute(Context As NameValueMap) Handles m_AutoSaveButton.OnExecute
            Try
                'string userName;
                'string userId = WebServicesUtils.GetUserId(out userName);
                Dim mgr As CWebServicesManager = New CWebServicesManager
                Dim isInitialize As Boolean = mgr.Initialize
                If (isInitialize = True) Then
                    'Dim userId As String = ""
                    'mgr.GetUserId(userId)
                    'Dim username As String = ""
                    'mgr.GetLoginUserName(username)
                    ''replace your App id here...
                    ''contact appsubmissions@autodesk.com for the App Id
                    'Dim appId As String = "3908021420381157341"
                    'Dim isValid As Boolean = Entitlement(appId, userId)
                    '' Get the auth token. 
                    'Dim Reg As RegistryKey = My.Computer.Registry.CurrentUser.OpenSubKey("Software\Autodesk\Inventor\Current Version\AutoSave", True)
                    Dim Settings As New Settings
                    'If isValid Then
                    '    Settings.ShowDialog()
                    'ElseIf Reg.GetValue("Valid") = True Then
                    '    System.Windows.Forms.MessageBox.Show("License-check bypassed by registry values")
                    '    Settings.ShowDialog()
                    'Else
                    '    System.Windows.Forms.MessageBox.Show("License-check fail")
                    Settings.ShowDialog()
                    'End If
                End If
            Catch ex As Exception
                System.Windows.Forms.MessageBox.Show(ex.Message)
            End Try

        End Sub
        Private Sub runAutoSave(Reg As RegistryKey)
            Dim InvProcess() As Process = Process.GetProcessesByName("Inventor")

            Do While Process.GetProcessesByName("Inventor").Count = 1
                If Reg.GetValue("AutoSave") = "True" Then
                    Dim interval As Integer = Reg.GetValue("Interval")
                    If interval <> Nothing AndAlso g_inventorApplication.Documents.VisibleDocuments.Count <> 0 Then
                        InvProcess = Process.GetProcessesByName("Inventor")
                        If InvProcess.Count > 1 Then
                            MessageBox.Show("AutoSave has detected multiple instances of Inventor" & vbNewLine &
                                                "This function can only work while a single instance is running")
                            Exit Sub
                        End If
                        Dim Time As TimeSpan = Now() - Process.GetCurrentProcess.StartTime
                        If CInt(Math.Round(Time.TotalSeconds / 60, 0) * 60) Mod interval = 0 Then
                            Try
                                While IsIdle = False
                                    Threading.Thread.Sleep(1000)
                                End While
                                SaveFiles(Reg, False)
                            Catch
                            End Try
                        End If
                    End If
                End If
                Threading.Thread.Sleep(60000)
            Loop
        End Sub
        Private Sub SaveFiles(Reg As RegistryKey, ByVal Clean As Boolean)

            Dim Proj As Integer = Reg.GetValue("Projects")
            If Clean = True Then
                Cleanup(g_inventorApplication.ActiveDocument, Reg)
            Else
                Select Case Proj
                    Case 0
                        DirtyWork(g_inventorApplication.ActiveDocument, Reg)
                    Case 1
                        For Each Document As Document In g_inventorApplication.Documents.VisibleDocuments
                            DirtyWork(Document, Reg)
                        Next
                    Case 2
                        For Each document In g_inventorApplication.Documents
                            DirtyWork(document, Reg)
                        Next
                End Select
            End If
        End Sub

        Private Sub DirtyWork(oDoc As Inventor.Document, Reg As RegistryKey)
            Dim Prop As Inventor.Property = Nothing
            If oDoc.Dirty = False Then Exit Sub
            If oDoc.FullFileName = "" Then
                Dim ans As MsgBoxResult
                ans = MsgBox("The document " & oDoc.DisplayName & " has not yet been saved." _
                    & vbNewLine & "Do you wish to save it now?", vbYesNo Or MsgBoxStyle.SystemModal, "AutoSave document " & oDoc.DisplayName)
                If ans = MsgBoxResult.Yes Then
                    Try
                        g_inventorApplication.ActiveDocument.Save()
                    Catch
                        Exit Sub
                    End Try
                End If
                Exit Sub
            End If
            Dim ReadCheck As New IO.FileInfo(oDoc.FullFileName)
                Dim Read As IO.FileInfo
            If ReadCheck.IsReadOnly = False Then
                Dim SaveName, Tag, Location, Directory As String
                Try
                    g_inventorApplication.SilentOperation = True
                    SkipSave = True
                    Dim customPropSet As Inventor.PropertySet
                    customPropSet = oDoc.PropertySets.Item("Inventor User Defined Properties")
                    Try
                        Prop = customPropSet.Add(oDoc.FullDocumentName, "Original")
                    Catch
                        Try
                            Prop.Value = oDoc.FullDocumentName
                        Catch
                        End Try
                    End Try

                    If Reg.GetValue("Use Document Location") = "True" Then
                        If Not My.Computer.FileSystem.DirectoryExists(IO.Path.GetDirectoryName(oDoc.FullFileName) & "\AutoSave") Then
                            My.Computer.FileSystem.CreateDirectory(IO.Path.GetDirectoryName(oDoc.FullFileName) & "\AutoSave")
                        End If
                        Location = oDoc.FullFileName
                        SaveName = Location.Insert(InStrRev(oDoc.FullFileName, "\"), "Autosave\")
                        Tag = GetTag(oDoc, IO.Path.GetDirectoryName(SaveName))
                        SaveName = SaveName.Insert(InStrRev(SaveName, "."), Tag & ".")
                        oDoc.SaveAs(SaveName, True)
                        Read = My.Computer.FileSystem.GetFileInfo(SaveName)
                        Read.IsReadOnly = True
                    Else
                        Location = Reg.GetValue("Save Location")
                        If Not My.Computer.FileSystem.DirectoryExists(Location) Then
                            My.Computer.FileSystem.CreateDirectory(Location)
                        End If
                        Tag = GetTag(oDoc, Location)
                        SaveName = Location & IO.Path.GetFileName(oDoc.FullFileName).Insert(InStrRev(IO.Path.GetFileName(oDoc.FullFileName), "."), Tag & ".")
                        oDoc.SaveAs(SaveName, True)
                        Read = My.Computer.FileSystem.GetFileInfo(SaveName)
                        Read.IsReadOnly = True
                    End If
                    Directory = IO.Path.GetDirectoryName(SaveName)
                    If Reg.GetValue("Keep Versions") = "True" Then
                        Dim dir As New DirectoryInfo(Directory)
                        Dim FileList As List(Of FileInfo) = dir.GetFiles("*" & IO.Path.GetFileNameWithoutExtension(oDoc.FullFileName) & "*").ToList
                        FileList.Sort(AddressOf SortByDate)
                        For X = 0 To FileList.Count - CInt(Reg.GetValue("Save Versions")) - 1
                            System.IO.File.SetAttributes(Directory & "\" & FileList.Item(X).ToString, FileAttributes.ReadOnly = False)
                            Kill(Directory & "\" & FileList.Item(X).ToString)
                        Next
                    ElseIf Reg.GetValue("Keep Older Than") = "True" Then
                        Dim SearchDir As New DirectoryInfo(Directory)
                        For Each file As FileInfo In SearchDir.GetFiles("*")
                            Dim span As TimeSpan = DateTime.Now.Subtract(file.CreationTime)
                            If span.TotalSeconds > Reg.GetValue("Old Interval") Then
                                file.IsReadOnly = False
                                Kill(file.FullName)
                            End If
                        Next
                    End If
                Catch ex As Exception
                Finally
                    Try
                        Prop.Delete()
                    Catch
                    End Try
                    g_inventorApplication.SilentOperation = False
                    SkipSave = False
                End Try
            End If
        End Sub
        Private Sub Cleanup(oDoc As Document, Reg As RegistryKey)
            Dim Location, Savename As String

            If Reg.GetValue("Use Document Location") = "True" Then
                If Not My.Computer.FileSystem.DirectoryExists((IO.Path.GetDirectoryName(oDoc.FullFileName) & "\AutoSave")) Then
                    My.Computer.FileSystem.CreateDirectory((IO.Path.GetDirectoryName(oDoc.FullFileName) & "\AutoSave"))
                End If
                Location = oDoc.FullFileName
                Savename = Location.Insert(InStrRev(oDoc.FullFileName, "\"), "Autosave\")
                Location = IO.Path.GetDirectoryName(Savename)
                Try
                    Dim SearchDir As New DirectoryInfo(Location)
                    If My.Computer.FileSystem.DirectoryExists(Location) Then
                        For Each file As FileInfo In SearchDir.GetFiles(IO.Path.GetFileNameWithoutExtension(oDoc.FullFileName) & ".????" & IO.Path.GetExtension(oDoc.FullFileName).ToString)
                            file.IsReadOnly = False
                            Kill(file.FullName)
                        Next
                        If Directory.GetFiles(Location, "*.*", SearchOption.AllDirectories).Length = 0 Then Directory.Delete(Location)
                    End If
                Catch ex As Exception
                    MessageBox.Show("Unable to clean " & Location & vbNewLine & "Some items will need to be deleted manually")
                End Try
            Else
                Location = Reg.GetValue("Save Location")
                Dim SearchDir As New DirectoryInfo(Location)
                Try
                    If My.Computer.FileSystem.DirectoryExists(Location) Then
                        For Each file As FileInfo In SearchDir.GetFiles(IO.Path.GetFileNameWithoutExtension(oDoc.FullFileName) & ".????" & IO.Path.GetExtension(oDoc.FullFileName).ToString)
                            file.IsReadOnly = False
                            Kill(file.FullName)
                        Next
                        If Directory.GetFiles(Location, "*.*", SearchOption.AllDirectories).Length = 0 Then Directory.Delete(Location)
                    End If
                Catch ex As Exception
                    MessageBox.Show("Unable to clean " & Location & vbNewLine & "Some items will need to be deleted manually" & vbNewLine & vbNewLine & ex.ToString)
                End Try
            End If

        End Sub
        Function GetTag(ByRef oDoc As Inventor.Document, ByRef Location As String) As String
            Dim dir As New DirectoryInfo(Location)
            Dim FileList As List(Of FileInfo) = dir.GetFiles(IO.Path.GetFileNameWithoutExtension(oDoc.FullFileName) & ".????" & IO.Path.GetExtension(oDoc.FullFileName)).ToList
            Dim List As String = Nothing
            FileList.Sort(AddressOf SortByDate)
            Dim Tag As Integer = 0
            If FileList.Count <> 0 Then
                Tag = (FileList.Item(FileList.Count - 1).ToString).Substring(Len(FileList.Item(FileList.Count - 1).ToString) - 8, 4)
            End If
            CStr(Tag + 1).PadLeft(4, "0")
            Return CStr(Tag + 1).PadLeft(4, "0")
        End Function
        Private Function SortByDate(X As FileInfo, Y As FileInfo) As Integer
            Return X.CreationTime.CompareTo(Y.CreationTime)
        End Function

        Private Sub m_AppEvents_OnSaveDocument(DocumentObject As _Document, BeforeOrAfter As EventTimingEnum, Context As NameValueMap, ByRef HandlingCode As HandlingCodeEnum) Handles m_AppEvents.OnSaveDocument
            If BeforeOrAfter <> EventTimingEnum.kAfter Then
                Exit Sub
            End If
            Dim Reg As RegistryKey = My.Computer.Registry.CurrentUser.OpenSubKey("Software\Autodesk\Inventor\Current Version\AutoSave", True)
            If Not Reg Is Nothing Then
                If Reg.GetValue("Cleanup") = "True" And SkipSave = False Then
                    SaveFiles(Reg, True)
                End If
            End If
        End Sub
        Private Sub m_AppEvents_OnOpenDocument(DocumentObject As _Document, FullDocumentName As String, BeforeOrAfter As EventTimingEnum, Context As NameValueMap, ByRef HandlingCode As HandlingCodeEnum) Handles m_AppEvents.OnOpenDocument
            Dim OpenVersion As New Open_Version
            If BeforeOrAfter = EventTimingEnum.kAfter Then
                Dim oDoc As Document = g_inventorApplication.ActiveDocument
                Dim Read As IO.FileInfo = My.Computer.FileSystem.GetFileInfo(oDoc.FullFileName)
                If Read.IsReadOnly = True Then
                    Dim Copy, Original As String
                    Try
                        Dim customPropSet As Inventor.PropertySet = oDoc.PropertySets.Item("Inventor User Defined Properties")
                        Dim Prop As Inventor.Property = customPropSet.Item("Original")
                        If customPropSet.Item("Original").Value = "" Then Exit Sub
                        g_inventorApplication.SilentOperation = True
                        Original = Prop.Value
                        OpenVersion.lblOriginal.Text = Original
                        Prop.Delete()
                        Copy = IO.Path.GetFileName(FullDocumentName)
                        For Each document In g_inventorApplication.Documents
                            If IO.Path.GetFileName(document.fullfilename) = IO.Path.GetFileName(Original) Then OpenVersion.rbRestore.Enabled = False
                            If Not My.Computer.FileSystem.FileExists(Original) Then OpenVersion.rbOpenCurrent.Enabled = False
                        Next
                        If Strings.Len(Original) > 45 Then Original = Strings.Left(Original, 45 - Strings.Len(IO.Path.GetFileName(Original))) & "...\" & IO.Path.GetFileName(Original)
                        If Strings.Len(Copy) > 45 Then Copy = Strings.Left(Copy, 45 - Strings.Len(IO.Path.GetFileName(Copy))) & "...\" & IO.Path.GetFileName(Copy)
                        OpenVersion.rtbInfo.Text = "You have opened an autosaved version (" & Copy & ") of the file (" & Original & "). If you proceed with opening the old version, you will not be allowed to save. You can restore the old version to be the current version if no other versions are open and the current version is not checked out."
                        OpenVersion.ShowDialog()
                    Catch
                    Finally
                        g_inventorApplication.SilentOperation = False
                    End Try
                End If
            End If

        End Sub

        Private Sub m_UIEvents2_OnActivateCommand(CommandName As String, Context As NameValueMap) Handles m_UIEvents2.OnActivateCommand
            IsIdle = False
        End Sub

        Private Sub m_UIEvents2_OnTerminateCommand(CommandName As String, Context As NameValueMap) Handles m_UIEvents2.OnTerminateCommand
            IsIdle = True
        End Sub
    End Class
#End Region
    Class WebServicesUtils

        Private Declare Function AdGetUserId Lib "AdWebServices" Alias "GetUserId" (ByVal userid As StringBuilder, ByVal buffersize As Integer) As Integer

        Private Declare Function AdIsWebServicesInitialized Lib "AdWebServices" Alias "IsWebServicesInitialized" () As Boolean

        Private Declare Sub AdInitializeWebServices Lib "AdWebServices" Alias "InitializeWebServices" ()

        Private Declare Function AdIsLoggedIn Lib "AdWebServices" Alias "IsLoggedIn" () As Boolean

        Private Declare Function AdGetLoginUserName Lib "AdWebServices" Alias "GetLoginUserName" (ByVal username As StringBuilder, ByVal buffersize As Integer) As Integer

        Friend Shared Function _GetUserId() As String
            Dim buffersize As Integer = 128
            'should be long enough for userid
            Dim sb As StringBuilder = New StringBuilder(buffersize)
            Dim len As Integer = WebServicesUtils.AdGetUserId(sb, buffersize)
            sb.Length = len
            Return sb.ToString
        End Function

        Friend Shared Function _GetUserName() As String
            Dim buffersize As Integer = 128
            'should be long enough for username 
            Dim sb As StringBuilder = New StringBuilder(buffersize)
            Dim len As Integer = WebServicesUtils.AdGetLoginUserName(sb, buffersize)
            sb.Length = len
            Return sb.ToString
        End Function

        Public Shared Function GetUserId(ByRef userName As String) As String
            WebServicesUtils.AdInitializeWebServices
            If Not WebServicesUtils.AdIsWebServicesInitialized Then
                Throw New Exception("Could not initialize the web services component.")
            End If

            If Not WebServicesUtils.AdIsLoggedIn Then
                Throw New Exception("User is not logged in. Please log-in to Autodesk 360")
            End If

            Dim userId As String = WebServicesUtils._GetUserId
            If (userId = "") Then
                Throw New Exception("Could not get user id. Please log-in to Autodesk 360")
            End If

            userName = WebServicesUtils._GetUserName
            If (userName = "") Then
                Throw New Exception("Could not get user name. Please log-in to Autodesk 360")
            End If

            Return userId
        End Function
    End Class
    <Serializable()>
    Public Class EntitlementResponse

        Public Property UserId As String
            Get
            End Get
            Set
            End Set
        End Property

        Public Property AppId As String
            Get
            End Get
            Set
            End Set
        End Property

        Public Property IsValid As Boolean
            Get
            End Get
            Set
            End Set
        End Property

        Public Property Message As String
            Get
            End Get
            Set
            End Set
        End Property
    End Class
End Namespace
Public Module Globals
    ' Inventor application object.
    Public g_inventorApplication As Inventor.Application
    Public IsIdle As Boolean

#Region "Function to get the add-in client ID."
    ' This function uses reflection to get the GuidAttribute associated with the add-in.
    Public Function AddInClientID() As String
        Dim guid As String = ""
        Try
            Dim t As Type = GetType(AutoSave.StandardAddInServer)
            Dim customAttributes() As Object = t.GetCustomAttributes(GetType(GuidAttribute), False)
            Dim guidAttribute As GuidAttribute = CType(customAttributes(0), GuidAttribute)
            guid = "{" + guidAttribute.Value.ToString() + "}"
        Catch
        End Try

        Return guid
    End Function
#End Region

#Region "hWnd Wrapper Class"
    ' This class is used to wrap a Win32 hWnd as a .Net IWind32Window class.
    ' This is primarily used for parenting a dialog to the Inventor window.
    '
    ' For example:
    ' myForm.Show(New WindowWrapper(g_inventorApplication.MainFrameHWND))
    '
    Public Class WindowWrapper
        Implements System.Windows.Forms.IWin32Window
        Public Sub New(ByVal handle As IntPtr)
            _hwnd = handle
        End Sub

        Public ReadOnly Property Handle() As IntPtr _
          Implements System.Windows.Forms.IWin32Window.Handle
            Get
                Return _hwnd
            End Get
        End Property

        Private _hwnd As IntPtr
    End Class
#End Region

#Region "Image Converter"
    ' Class used to convert bitmaps and icons from their .Net native types into
    ' an IPictureDisp object which is what the Inventor API requires. A typical
    ' usage is shown below where MyIcon is a bitmap or icon that's available
    ' as a resource of the project.
    '
    'Dim smallIcon As stdole.IPictureDisp = PictureDispConverter.ToIPictureDisp(My.Resources.MyIcon)

    Public NotInheritable Class PictureDispConverter
        <DllImport("OleAut32.dll", EntryPoint:="OleCreatePictureIndirect", ExactSpelling:=True, PreserveSig:=False)>
        Private Shared Function OleCreatePictureIndirect(
            <MarshalAs(UnmanagedType.AsAny)> ByVal picdesc As Object,
            ByRef iid As Guid,
            <MarshalAs(UnmanagedType.Bool)> ByVal fOwn As Boolean) As stdole.IPictureDisp
        End Function

        Shared iPictureDispGuid As Guid = GetType(stdole.IPictureDisp).GUID

        Private NotInheritable Class PICTDESC
            Private Sub New()
            End Sub

            'Picture Types
            Public Const PICTYPE_BITMAP As Short = 1
            Public Const PICTYPE_ICON As Short = 3

            <StructLayout(LayoutKind.Sequential)>
            Public Class Icon
                Friend cbSizeOfStruct As Integer = Marshal.SizeOf(GetType(PICTDESC.Icon))
                Friend picType As Integer = PICTDESC.PICTYPE_ICON
                Friend hicon As IntPtr = IntPtr.Zero
                Friend unused1 As Integer
                Friend unused2 As Integer

                Friend Sub New(ByVal icon As System.Drawing.Icon)
                    Me.hicon = icon.ToBitmap().GetHicon()
                End Sub
            End Class

            <StructLayout(LayoutKind.Sequential)>
            Public Class Bitmap
                Friend cbSizeOfStruct As Integer = Marshal.SizeOf(GetType(PICTDESC.Bitmap))
                Friend picType As Integer = PICTDESC.PICTYPE_BITMAP
                Friend hbitmap As IntPtr = IntPtr.Zero
                Friend hpal As IntPtr = IntPtr.Zero
                Friend unused As Integer

                Friend Sub New(ByVal bitmap As System.Drawing.Bitmap)
                    Me.hbitmap = bitmap.GetHbitmap()
                End Sub
            End Class
        End Class

        Public Shared Function ToIPictureDisp(ByVal icon As System.Drawing.Icon) As stdole.IPictureDisp
            Dim pictIcon As New PICTDESC.Icon(icon)
            Return OleCreatePictureIndirect(pictIcon, iPictureDispGuid, True)
        End Function

        Public Shared Function ToIPictureDisp(ByVal bmp As System.Drawing.Bitmap) As stdole.IPictureDisp
            Dim pictBmp As New PICTDESC.Bitmap(bmp)
            Return OleCreatePictureIndirect(pictBmp, iPictureDispGuid, True)
        End Function
    End Class
#End Region

End Module
