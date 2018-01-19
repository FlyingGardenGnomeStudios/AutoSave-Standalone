
Imports System.Windows.Forms
Imports Microsoft.Win32

Public Class Settings
    Public Sub New()
        InitializeComponent()
        Dim Reg As Microsoft.Win32.RegistryKey = My.Computer.Registry.CurrentUser.OpenSubKey("Software\Autodesk\Inventor\Current Version\AutoSave", True)
        If Reg Is Nothing Then
            Reg = My.Computer.Registry.CurrentUser.CreateSubKey("Software\Autodesk\Inventor\Current Version\AutoSave")

            cmbProjects.SelectedIndex = My.Settings.cmbProj
            cmbTime.SelectedIndex = My.Settings.cmbInt
            cmbOlderThan.SelectedIndex = My.Settings.cmbOld
            numInt.Value = My.Settings.PropertyValues("SaveInt").Property.DefaultValue
            numVer.Value = My.Settings.PropertyValues("SaveVer").Property.DefaultValue
            numOld.Value = My.Settings.PropertyValues("SaveOld").Property.DefaultValue
            chkDocLoc.Checked = My.Settings.PropertyValues("DocLoc").Property.DefaultValue
            ChkAutoSave.Checked = My.Settings.PropertyValues("AutoSave").Property.DefaultValue
            rdoVersions.Checked = My.Settings.PropertyValues("KeepVersions").Property.DefaultValue
            rdoOlderThan.Checked = My.Settings.PropertyValues("KeepOlderThan").Property.DefaultValue
            rdoKeepEverything.Checked = My.Settings.PropertyValues("Keep Everything").Property.DefaultValue
            chkClean.Checked = My.Settings.PropertyValues("Cleanup").Property.DefaultValue
        Else
            cmbProjects.SelectedIndex = Reg.GetValue("Projects")
            numInt.Value = Reg.GetValue("Save Interval")
            cmbTime.SelectedIndex = Reg.GetValue("Interval Type")
            txtSaveLoc.Text = Reg.GetValue("Save Location")
            chkDocLoc.Checked = Reg.GetValue("Use Document Location")
            numVer.Value = Reg.GetValue("Save Versions")
            cmbOlderThan.SelectedIndex = Reg.GetValue("Old Files")
            numOld.Value = Reg.GetValue("Save Old")
            ChkAutoSave.Checked = Reg.GetValue("Autosave")
            rdoVersions.Checked = Reg.GetValue("Keep Versions")
            rdoOlderThan.Checked = Reg.GetValue("Keep Older Than")
            rdoKeepEverything.Checked = Reg.GetValue("Keep Everything")
            chkClean.Checked = Reg.GetValue("Cleanup")
        End If

    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.Close()
    End Sub
    Private Sub rdoVersions_CheckedChanged(sender As Object, e As EventArgs) Handles rdoVersions.CheckedChanged
        If rdoVersions.Checked = True Then
            cmbOlderThan.Enabled = False
            numOld.Enabled = False
            numVer.Enabled = True
        End If
    End Sub
    Private Sub rdoOlderThan_CheckedChanged(sender As Object, e As EventArgs) Handles rdoOlderThan.CheckedChanged
        If rdoOlderThan.Checked = True Then
            cmbOlderThan.Enabled = True
            numOld.Enabled = True
            numVer.Enabled = False
        End If
    End Sub
    Private Sub rdoKeepEverything_CheckedChanged(sender As Object, e As EventArgs) Handles rdoKeepEverything.CheckedChanged
        If rdoKeepEverything.Checked = True Then
            cmbOlderThan.Enabled = False
            numOld.Enabled = False
            numVer.Enabled = False
        End If
    End Sub
    Private Sub btnBrowse_Click(sender As Object, e As EventArgs) Handles btnBrowse.Click
        Dim Folder As FolderBrowserDialog = New FolderBrowserDialog
        Folder.Description = "Choose the location you wish to save your files"
        Folder.RootFolder = System.Environment.SpecialFolder.Desktop
        Try
            Folder.SelectedPath = txtSaveLoc.Text
            If Folder.ShowDialog() = Windows.Forms.DialogResult.OK Then
                txtSaveLoc.Text = Folder.SelectedPath & "\"
            End If
        Catch ex As Exception
            MessageBox.Show(ex.Message, "Exception Details", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
        End Try
    End Sub

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles chkDocLoc.CheckedChanged
        If chkDocLoc.Checked = True Then
            btnBrowse.Enabled = False
            txtSaveLoc.Enabled = False
        Else
            btnBrowse.Enabled = True
            txtSaveLoc.Enabled = True
        End If
    End Sub

    Private Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click
        If chkDocLoc.Checked = False And txtSaveLoc.Text = "" Then
            MsgBox("Invalid save location" & vbNewLine & "Enter a valid location or select 'Use Document Location'")
            Exit Sub
        ElseIf chkDocLoc.Checked = False Then
            Try
                My.Computer.FileSystem.CreateDirectory(txtSaveLoc.Text)
            Catch ex As Exception
                MessageBox.Show(ex.Message)
            End Try
        End If
        Dim Reg As Microsoft.Win32.RegistryKey = My.Computer.Registry.CurrentUser.OpenSubKey("Software\Autodesk\Inventor\Current Version\AutoSave", True)
        Dim Interval As Integer
        Select Case cmbTime.SelectedIndex
            Case 0
                Interval = numInt.Value * 60
            Case 1
                Interval = numInt.Value * 3600
            Case 2
                Interval = numInt.Value * 86400
        End Select
        Reg.SetValue("Interval", Interval, RegistryValueKind.QWord)
        Reg.SetValue("Projects", cmbProjects.SelectedIndex, RegistryValueKind.DWord)
        Reg.SetValue("Save Interval", numInt.Value, RegistryValueKind.DWord)
        Reg.SetValue("Interval Type", cmbTime.SelectedIndex, RegistryValueKind.DWord)
        Reg.SetValue("Save Location", txtSaveLoc.Text, RegistryValueKind.String)
        Reg.SetValue("Use Document Location", chkDocLoc.Checked, RegistryValueKind.String)
        Reg.SetValue("Save Versions", numVer.Value, RegistryValueKind.DWord)
        Reg.SetValue("Old Files", cmbOlderThan.SelectedIndex, RegistryValueKind.DWord)
        Reg.SetValue("Save Old", numOld.Value, RegistryValueKind.DWord)
        Select Case cmbOlderThan.SelectedIndex
            Case 0
                Interval = numOld.Value * 60
            Case 1
                Interval = numOld.Value * 3600
            Case 2
                Interval = numOld.Value * 86400
        End Select
        Reg.SetValue("Old Interval", Interval, RegistryValueKind.DWord)
        Reg.SetValue("AutoSave", ChkAutoSave.Checked, RegistryValueKind.String)
        Reg.SetValue("Keep Versions", rdoVersions.Checked, RegistryValueKind.String)
        Reg.SetValue("Keep Older Than", rdoOlderThan.Checked, RegistryValueKind.String)
        Reg.SetValue("Keep Everything", rdoKeepEverything.Checked, RegistryValueKind.String)
        Reg.SetValue("Cleanup", chkClean.Checked, RegistryValueKind.String)
        Me.Close()
    End Sub

    Private Sub btnDefaults_Click(sender As Object, e As EventArgs) Handles btnDefaults.Click
        LoadDefaults()
    End Sub

    Private Sub ChkAutoSave_CheckedChanged(sender As Object, e As EventArgs) Handles ChkAutoSave.CheckedChanged
        If ChkAutoSave.Checked = True Then
            cmbProjects.Enabled = True
            gbSaveInterval.Enabled = True
            gbSaveLoc.Enabled = True
            chkDocLoc.Enabled = True
            chkClean.Enabled = True
        Else
            cmbProjects.Enabled = False
            gbSaveInterval.Enabled = False
            gbSaveLoc.Enabled = False
            chkDocLoc.Enabled = False
            chkClean.Enabled = False
        End If
        If numInt.Value = 0 Then
            LoadDefaults()
        End If
    End Sub
    Private Sub LoadDefaults()
        cmbProjects.SelectedIndex = My.Settings.cmbProj
        cmbTime.SelectedIndex = My.Settings.cmbInt
        cmbOlderThan.SelectedIndex = My.Settings.cmbOld
        numInt.Value = My.Settings.PropertyValues("SaveInt").Property.DefaultValue
        numVer.Value = My.Settings.PropertyValues("SaveVer").Property.DefaultValue
        numOld.Value = My.Settings.PropertyValues("SaveOld").Property.DefaultValue
        chkDocLoc.Checked = My.Settings.PropertyValues("DocLoc").Property.DefaultValue
        rdoVersions.Checked = My.Settings.PropertyValues("KeepVersions").Property.DefaultValue
        rdoOlderThan.Checked = My.Settings.PropertyValues("KeepOlderThan").Property.DefaultValue
        chkClean.Checked = My.Settings.PropertyValues("Cleanup").Property.DefaultValue
        txtSaveLoc.Text = My.Settings.PropertyValues("SaveLoc").Property.DefaultValue
        chkDocLoc.Checked = True
    End Sub
End Class
