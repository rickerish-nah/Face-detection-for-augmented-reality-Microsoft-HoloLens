using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
using UnityEngine.XR.WSA.WebCam;


class ImageCapture : MonoBehaviour
{

    public static ImageCapture instance; /// Allows this class to behave like a singleton
    private int tapsCount; /// Keeps track of tapCounts to name the captured images
    private PhotoCapture photoCaptureObject = null; /// PhotoCapture object used to capture images on HoloLens 

    private void Awake() /// Initialises this class
    {
        instance = this;
    }
    
    private void TapHandler(TappedEventArgs obj) /// Respond to Tap Input.
    {
        tapsCount++;
        ExecuteImageCaptureAndAnalysis();
    }

    private void ExecuteImageCaptureAndAnalysis() /// Begin process of Image Capturing and send To Azure Computer Vision service.
    {
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;

            CameraParameters c = new CameraParameters();
            c.hologramOpacity = 0.0f;
            c.cameraResolutionWidth = targetTexture.width;
            c.cameraResolutionHeight = targetTexture.height;
            c.pixelFormat = CapturePixelFormat.BGRA32;

            captureObject.StartPhotoModeAsync(c, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                string filename = string.Format(@"CapturedImage{0}.jpg", tapsCount);
                string filePath = Path.Combine(Application.persistentDataPath, filename);

                // Set the image path on the FaceAnalysis class
                FaceAnalysis.Instance.imagePath = filePath;

                photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
            });
        });
    }

    void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result) /// Called right after the photo capture process has concluded
    {
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode); // Deactivate the camera
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result) /// Register the full execution of the Photo Capture. If successfull, it will begin the Image Analysis process.
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;

        // Request image caputer analysis
        StartCoroutine(FaceAnalysis.Instance.DetectFacesFromImage());
    }
}