Imports System.Threading
Imports System.Net
Imports System.IO

Imports Emgu

Public Class CameraPlug
    Implements IDisposable

    Public Enum PlugStatus
        Idle = 0
        Playing = 1
    End Enum

    Public Enum Directions
        Invalid = -1
        Up = 0
        StopUp = 1
        Down = 2
        StopDown = 3
        Left = 4
        StopLeft = 5
        Right = 6
        StopRight = 7
    End Enum

    Private mHostName As String
    Private mPort As Integer
    Private mUserName As String
    Private mPassword As String
    Private mHasAdminAccess As Boolean
    Private mResolution As Resolution = New Resolution(640, 480)
    Private mCompression As Short = 30
    Private mFramesPerSecond As Short = 30
    Private mActualFramesPerSecond As Short
    Private mLastFrame As Image = New Bitmap(1, 1)
    Private mStatus As PlugStatus
    Private mFaceDetection As Boolean

    Private mVideoScript As String
    Private mControlScript As String
    Private mPTZScript As String

    Private framesReceived As Integer
    Private resetVideoLoop As Boolean

    Private videoRequest As New WebClient()
    Private ptzControl As New WebClient()
    Private cacheAuth As CredentialCache = New CredentialCache()

    Private cancelThreads As Boolean
    Private getVideoThread As Thread
    Private fpsCounterThread As Thread
    Private autoResetEvent As AutoResetEvent

    Private buffer() As Byte

    Private listener As HttpListener

    Private frontFaceDetector As CV.CascadeClassifier
    Private profileFaceDetector As CV.CascadeClassifier
    Private minFaceSize As New Size(30, 30)
    Private faceDetColor As New Pen(Color.FromArgb(164, Color.Magenta), 4)
    Private faceRects As New List(Of Rectangle)
    Private shapeDetColor As New Pen(Color.FromArgb(128, Color.Blue), 4)

    Public Event NewFrame(sender As Object, image As Image)

    Private Enum AlignType
        SearchImageStart = 0
        SearchImageEnd = 1
    End Enum

    Public Sub New()
        ' http://alereimondo.no-ip.org/OpenCV/34
        Dim path = IO.Path.Combine(My.Application.Info.DirectoryPath, "Resources")
        If Not path.EndsWith("\") Then path += "\"
        frontFaceDetector = New CV.CascadeClassifier(path + "haarcascade_frontalface_default.xml")
        frontFaceDetector = New CV.CascadeClassifier(path + "haarcascade_frontalface_alt_tree.xml")
        profileFaceDetector = New CV.CascadeClassifier(path + "haarcascade_profileface.xml")

        autoResetEvent = New AutoResetEvent(False)
    End Sub

    Public Sub New(hostName As String, videoScript As String, port As Integer, userName As String, password As String)
        Me.New()
        Me.Configure(hostName, videoScript, port, userName, password)
    End Sub

    Public Sub Connect()
        If getVideoThread IsNot Nothing Then Close()

        getVideoThread = New Thread(AddressOf GetVideoStreamLoop)
        getVideoThread.Start()

        fpsCounterThread = New Thread(AddressOf FPSCounter)
        fpsCounterThread.Priority = ThreadPriority.Lowest
        fpsCounterThread.Start()
    End Sub

    Public Sub Connect(hostName As String, videoScript As String, port As Integer, userName As String, password As String)
        Me.Configure(hostName, videoScript, port, userName, password)

        Me.Connect()
    End Sub

    Public Sub Close()
        cancelThreads = True
        getVideoThread = Nothing
    End Sub

    Public Sub Configure(hostName As String, videoScript As String, port As Integer, userName As String, password As String)
        Me.HostName = hostName
        Me.VideoScript = videoScript
        Me.Port = port
        Me.UserName = userName
        Me.Password = password
    End Sub

    Public Property HostName() As String
        Get
            Return mHostName
        End Get
        Set(ByVal value As String)
            mHostName = value
            resetVideoLoop = (mStatus = PlugStatus.Playing)
        End Set
    End Property

    Public Property VideoScript As String
        Get
            Return mVideoScript
        End Get
        Set(value As String)
            mVideoScript = value
            resetVideoLoop = (mStatus = PlugStatus.Playing)
        End Set
    End Property

    Public Property ControlScript As String
        Get
            Return mControlScript
        End Get
        Set(value As String)
            mControlScript = value
            resetVideoLoop = (mStatus = PlugStatus.Playing)
        End Set
    End Property

    Public Property PTZScript As String
        Get
            Return mPTZScript
        End Get
        Set(value As String)
            mPTZScript = value
            resetVideoLoop = (mStatus = PlugStatus.Playing)
        End Set
    End Property

    Public Property Port() As Integer
        Get
            Return mPort
        End Get
        Set(ByVal value As Integer)
            mPort = value
            resetVideoLoop = (mStatus = PlugStatus.Playing)
        End Set
    End Property

    Public Property UserName() As String
        Get
            Return mUserName
        End Get
        Set(ByVal value As String)
            mUserName = value
        End Set
    End Property

    Public Property Password() As String
        Get
            Return mPassword
        End Get
        Set(ByVal value As String)
            mPassword = value
            resetVideoLoop = (mStatus = PlugStatus.Playing)
        End Set
    End Property

    Public Property Resolution() As Resolution
        Get
            Return mResolution
        End Get
        Set(ByVal value As Resolution)
            mResolution = value

            minFaceSize = New Size(mResolution.Width / 10, mResolution.Height / 10)

            resetVideoLoop = (mStatus = PlugStatus.Playing)
        End Set
    End Property

    Public Property FramesPerSecond() As Short
        Get
            Return mFramesPerSecond
        End Get
        Set(ByVal value As Short)
            mFramesPerSecond = value
            resetVideoLoop = (mStatus = PlugStatus.Playing)
        End Set
    End Property

    Public Property Compression() As Short
        Get
            Return mCompression
        End Get
        Set(ByVal value As Short)
            mCompression = value
            resetVideoLoop = (mStatus = PlugStatus.Playing)
        End Set
    End Property

    Public ReadOnly Property HasAdminAccess() As Boolean
        Get
            Return mHasAdminAccess
        End Get
    End Property

    Public ReadOnly Property LastFrame() As Image
        Get
            Return mLastFrame
        End Get
    End Property

    Public ReadOnly Property ActualFramesPerSecond() As Short
        Get
            Return mActualFramesPerSecond
        End Get
    End Property

    Public ReadOnly Property Status() As PlugStatus
        Get
            Return mStatus
        End Get
    End Property

    Public Property FaceDetection As Boolean
        Get
            Return mFaceDetection
        End Get
        Set(value As Boolean)
            mFaceDetection = value
        End Set
    End Property

    Private Sub FPSCounter()
        Dim lastFramesReceived As Integer = 0

        Do Until cancelThreads
            autoResetEvent.WaitOne(1000)

            mActualFramesPerSecond = CShort(framesReceived - lastFramesReceived)

            If framesReceived >= 10000 Then framesReceived = 0
            lastFramesReceived = framesReceived
        Loop
    End Sub

    Private Sub HandleConnection(ar As IAsyncResult)
        Dim listener As HttpListener = CType(ar.AsyncState, HttpListener)
        If Not listener.IsListening Then Exit Sub
        Dim context As HttpListenerContext = listener.EndGetContext(ar)
        Dim user As System.Security.Principal.IPrincipal = context.User
        Dim response As HttpListenerResponse = context.Response

        Debug.WriteLine(String.Format("Requesting {0} from {1}", context.Request.Url, context.Request.UserAgent))

        response.KeepAlive = True
        response.SendChunked = True

        response.ContentType = "multipart/x-mixed-replace"
        response.ContentLength64 = buffer.Length
        Try
            response.OutputStream.Write(buffer, 0, buffer.Length)
            response.Close()
        Catch
        End Try

        'If user.Identity.Name = mUserName Then

        'Else
        '    context.Response.Headers.Add(HttpRequestHeader.Authorization, "Failed")
        'End If
    End Sub

    Private Sub GetVideoStreamLoop()
        Dim netColor As New HLSRGB(Color.Red)
        Dim ocvHue As Double = netColor.Hue / 360 * 180

        Const readSize As Integer = 1024 * 8 ' Adjust this value to control bandwidth
        Const bufferSize As Integer = 512 * readSize

        'Dim tmpImg As Image = Image.FromFile("\\MEDIA-CENTER\Users\Xavier\Pictures\Family Pictures\Familia FD variada\Pachy Gurabo- Marz'07.jpg")
        'Dim tmpImg As Image = Image.FromFile("C:\Windows\System32\oobe\background.bmp")
        Do
            Dim baseUri As Uri = New Uri(String.Format("{0}:{1}", mHostName, mPort))
            Dim videoUri As Uri = New Uri(String.Format("{0}:{1}/{2}", mHostName, mPort, mVideoScript))

            Dim wc As New WebClient()
            If cacheAuth.GetCredential(baseUri, "Basic") Is Nothing Then
                cacheAuth.Add(baseUri, "Basic", New NetworkCredential(mUserName, mPassword))
                ptzControl.Credentials = cacheAuth
            End If

            Dim request As HttpWebRequest = CType(WebRequest.Create(videoUri), HttpWebRequest)
            If mUserName <> "" OrElse mPassword <> "" Then request.Credentials = New NetworkCredential(mUserName, mPassword)
            Dim response As WebResponse = Nothing

            Try
                response = request.GetResponse()
            Catch ex As Exception
                MsgBox(ex.Message, MsgBoxStyle.Critical)
                Exit Sub
            End Try

            Dim responseType = response.ContentType
            If Not responseType.Contains("multipart/x-mixed-replace") Then Throw New ApplicationException("Invalid URL or Unsupported Media Format")

            If mControlScript <> "" Then
                Dim setParameter = Sub(paramName As String, paramValue As String)
                                       Dim controlUri As New Uri($"{mHostName}:{mPort}/{mControlScript}?param={paramName}&value={paramValue}")
                                       ptzControl.DownloadString(controlUri)
                                   End Sub

                setParameter("resolution", mResolution.Value)
            End If

            listener = New HttpListener With {
                .AuthenticationSchemes = AuthenticationSchemes.Anonymous
            }
            'listener.Prefixes.Add("http://+:2222/video/")
            listener.Start()
            listener.BeginGetContext(New AsyncCallback(AddressOf HandleConnection), listener)

            Dim boundary() As Byte = System.Text.ASCIIEncoding.ASCII.GetBytes(responseType.Substring(responseType.IndexOf("boundary=", 0) + 9))
            Dim boundaryLen As Integer = boundary.Length

            Dim stream As Stream = response.GetResponseStream()

            Dim totalRead As Integer = 0
            Dim currentPos As Integer = 0
            Dim remainingBufferLen As Integer = 0
            Dim bytesRead As Integer = 0
            Dim delimiter() As Byte = Nothing
            Dim delimiterLen As Integer = 0
            Dim delimiterLinux() As Byte = New Byte() {10, 10}
            Dim delimiterWin() As Byte = New Byte() {13, 10, 13, 10}
            Dim alignMode As AlignType = AlignType.SearchImageStart
            Dim startPos As Integer = 0
            Dim stopPos As Integer = 0
            Dim delay As Double

            ReDim buffer(bufferSize - 1)

            Dim sw As New Stopwatch()
            sw.Start()

            resetVideoLoop = False
            mStatus = PlugStatus.Playing

            Do
                If totalRead > bufferSize - readSize Then
                    totalRead = 0
                    currentPos = 0
                    remainingBufferLen = 0
                End If

                bytesRead = stream.Read(buffer, totalRead, readSize)
                If bytesRead = 0 Then Throw New ApplicationException()

                totalRead += bytesRead
                remainingBufferLen += bytesRead

                If delimiterLen = 0 Then
                    currentPos = FindArrayInArray(buffer, boundary, boundaryLen, currentPos, remainingBufferLen)
                    If currentPos = -1 Then
                        remainingBufferLen = boundaryLen - 1
                        currentPos = totalRead - remainingBufferLen
                        Continue Do
                    End If

                    remainingBufferLen = totalRead - currentPos
                    If remainingBufferLen < 2 Then Continue Do

                    If buffer(currentPos + boundaryLen) = 10 Then
                        delimiter = delimiterLinux
                    Else
                        delimiter = delimiterWin
                    End If
                    delimiterLen = delimiter.Length

                    currentPos += (boundaryLen + delimiterLen \ 2)
                    remainingBufferLen = totalRead - currentPos
                End If

                If alignMode = AlignType.SearchImageStart Then
                    startPos = FindArrayInArray(buffer, delimiter, delimiterLen, currentPos, remainingBufferLen)
                    If startPos <> -1 Then
                        startPos += delimiterLen
                        currentPos = startPos
                        remainingBufferLen = totalRead - currentPos
                        alignMode = AlignType.SearchImageEnd
                    Else
                        remainingBufferLen = delimiterLen - 1
                        currentPos = totalRead - remainingBufferLen
                    End If
                End If

                While (alignMode = AlignType.SearchImageEnd AndAlso remainingBufferLen >= boundaryLen) AndAlso Not cancelThreads
                    stopPos = FindArrayInArray(buffer, boundary, boundaryLen, currentPos, remainingBufferLen)
                    If stopPos <> -1 Then
                        currentPos = stopPos
                        remainingBufferLen = totalRead - currentPos

                        sw.Restart()
                        Using ms As MemoryStream = New MemoryStream(buffer, startPos, stopPos - startPos)
                            Try
                                mLastFrame = Image.FromStream(ms)

                                If mFaceDetection Then
                                    'Dim contours As Emgu.CV.Contour(Of Point)

                                    'Using hsvImg As New CV.Image(Of CV.Structure.Hsv, Byte)(mLastFrame)
                                    '    Using filteredImage = hsvImg.SmoothGaussian(3).InRange(
                                    '                                                    New CV.Structure.Hsv(165, 120, 40),
                                    '                                                    New CV.Structure.Hsv(180, 255, 255)).SmoothGaussian(9)
                                    '        'Using filteredImage = hsvImg.SmoothGaussian(3).InRange(
                                    '        '                                        New CV.Structure.Hsv(ocvHue - 20, 130, 40),
                                    '        '                                        New CV.Structure.Hsv(ocvHue, 255, 255)).SmoothGaussian(9)
                                    '        contours = filteredImage.FindContours(CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                                    '                                                      CV.CvEnum.RETR_TYPE.CV_RETR_LIST)
                                    '        End Using
                                    '    End Using

                                    Using g As Graphics = Graphics.FromImage(mLastFrame)
                                        g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

                                        Using imgGray As New CV.Image(Of CV.Structure.Gray, Byte)(mLastFrame)
                                            faceRects.Clear()

                                            ProcessClassifier(frontFaceDetector, imgGray)
                                            ProcessClassifier(profileFaceDetector, imgGray, 1.3)

                                            faceRects.ForEach(Sub(rect) g.DrawEllipse(faceDetColor, rect))
                                            'faceRects.ForEach(Sub(rect) g.DrawRectangle(faceDetColor, rect))
                                        End Using

                                        'Using gp As New Drawing2D.GraphicsPath()
                                        '    While contours IsNot Nothing
                                        '        Dim result = contours.ApproxPoly(5)

                                        '        If result.Count > 2 Then
                                        '            Dim points As New List(Of Point)
                                        '            For i = 0 To result.Count - 1
                                        '                points.Add(result(i))
                                        '            Next
                                        '            gp.AddPolygon(points.ToArray())
                                        '            gp.CloseFigure()
                                        '        End If

                                        '        contours = contours.HNext
                                        '    End While

                                        '    g.SetClip(gp)
                                        '    g.DrawImage(tmpImg, 0, 0, mResolution.Width, mResolution.Height)
                                        '    'g.FillPath(Brushes.Yellow, gp)
                                        'End Using
                                    End Using
                                End If

                                RaiseEvent NewFrame(Me, mLastFrame)
                            Catch
                            End Try
                        End Using
                        sw.Stop()

                        framesReceived += 1
                        If mFramesPerSecond > 0 Then
                            delay = 1 / mFramesPerSecond * 1000 - sw.ElapsedMilliseconds
                            If delay > 0 Then autoResetEvent.WaitOne(delay)
                        End If

                        currentPos = stopPos + boundaryLen
                        remainingBufferLen = totalRead - currentPos
                        Array.Copy(buffer, currentPos, buffer, 0, remainingBufferLen)

                        totalRead = remainingBufferLen
                        currentPos = 0
                        alignMode = AlignType.SearchImageStart
                    Else
                        remainingBufferLen = boundaryLen - 1
                        currentPos = totalRead - remainingBufferLen
                    End If
                End While
            Loop Until cancelThreads OrElse resetVideoLoop

            response.Close()
            response.Dispose()
            request.Abort()

            listener.Stop()
        Loop Until cancelThreads

        mStatus = PlugStatus.Idle
    End Sub

    Private Sub ProcessClassifier(classifier As CV.CascadeClassifier, imgGray As CV.Image(Of CV.Structure.Gray, Byte), Optional scaleFactor As Double = 1.2, Optional minNeighbors As Integer = 3)
        Dim containsPoint As Boolean

        For Each face As Rectangle In classifier.DetectMultiScale(imgGray,
                                                                    scaleFactor,
                                                                    minNeighbors,
                                                                    minFaceSize,
                                                                    Size.Empty)
            containsPoint = False
            Dim c = face.CircleCenter()
            For Each rc In faceRects
                If rc.CircleContainsPoint(c) Then
                    containsPoint = True
                    Exit For
                End If
            Next
            If Not containsPoint Then faceRects.Add(face)
        Next
    End Sub

    Private Function FindArrayInArray(byteArray() As Byte, needle() As Byte, needleLen As Integer, startIndex As Integer, count As Integer) As Integer
        Dim index As Integer
        Dim i As Integer

        While count >= needleLen
            index = Array.IndexOf(Of Byte)(byteArray, needle(0), startIndex, count - needleLen + 1)
            If index = -1 Then Exit While

            For i = 0 To needleLen - 1
                If byteArray(index + i) <> needle(i) Then
                    count -= (index - startIndex + 1)
                    startIndex = index + 1
                    Continue While
                End If
            Next

            Return index
        End While

        Return -1
    End Function

    Public Sub Move(direction As Directions, Optional speed As Integer = 8)
        If direction = Directions.Invalid Then Exit Sub

        Dim directionValue As Integer = direction
        Dim ptzUri As New Uri(String.Format(String.Format("{0}:{1}/{2}?command={3}&onestep=1&degree={4}", mHostName,
                                                                                        mPort,
                                                                                        mPTZScript,
                                                                                        directionValue, speed)))
        ptzControl.DownloadString(ptzUri)
    End Sub

#Region " IDisposable Support "
    Private disposedValue As Boolean = False        ' To detect redundant calls

    ' IDisposable
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If disposing Then
                ' TODO: free other state (managed objects).

                cancelThreads = True
                autoResetEvent.Set()
            End If

            ' TODO: free your own state (unmanaged objects).
            ' TODO: set large fields to null.
        End If
        Me.disposedValue = True
    End Sub

    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region
End Class