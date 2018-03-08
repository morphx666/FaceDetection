Imports System.Threading

Public Class FormMain
    Private Delegate Sub UpdateFPSDel()
    Private cameraPlug As NPlug.CameraPlug
    Private cancelThreads As Boolean
    Private autoEvent As AutoResetEvent

    Private Sub FormMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        cancelThreads = True
        autoEvent.Set()

        cameraPlug.Dispose()
    End Sub

    Private Sub FormMain_Load(sender As System.Object, e As EventArgs) Handles MyBase.Load
        CameraViewerMain.Location = Point.Empty
        Me.Size = New Size(CameraViewerMain.Width + 16, CameraViewerMain.Height + 32 + 4)
        CameraViewerMain.AutoSize = False

        'cameraPlug = New NPlug.CameraPlug("http://192.168.1.107", "videostream.cgi", 80, "xfx", "test-pwd")
        'cameraPlug = New NPlug.CameraPlug("http://192.168.1.104", "mjpg/video.mjpg", 80, "root", "test-pwd")
        cameraPlug = New NPlug.CameraPlug("http://192.168.1.101", "videostream.cgi", 8085, "xfx", "test-pwd")

        cameraPlug.ControlScript = "camera_control.cgi"
        cameraPlug.PTZScript = "decoder_control.cgi"
        'cameraPlug.PTZScript = "/axis-cgi/com/ptz.cgi"

        cameraPlug.FramesPerSecond = 15
        cameraPlug.Compression = 60

        'cameraPlug.Resolution = New NPlug.Resolution("320x240")
        cameraPlug.Resolution = New NPlug.Resolution("640x480")
        'cameraPlug.Resolution = New NPlug.Resolution("800x600")
        'cameraPlug.Resolution = New NPlug.Resolution("1024x768")
        'cameraPlug.Resolution = New NPlug.Resolution("1280x720")

        cameraPlug.FaceDetection = True

        CameraViewerMain.CameraPlug = cameraPlug

        cameraPlug.Connect()

        autoEvent = New AutoResetEvent(False)
        Dim monitorFPSThread As Thread = New Thread(AddressOf MonitorFPSSub)
        monitorFPSThread.Priority = ThreadPriority.BelowNormal
        monitorFPSThread.Start()
    End Sub

    Private Sub MonitorFPSSub()
        Do
            autoEvent.WaitOne(500, True)
            If cancelThreads Then Exit Do

            Try
                Me.Invoke(New MethodInvoker(Sub() Me.Text = cameraPlug.ActualFramesPerSecond.ToString()))
            Catch
                Exit Sub
            End Try
        Loop
    End Sub
End Class