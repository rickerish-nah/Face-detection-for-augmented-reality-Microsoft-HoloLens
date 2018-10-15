using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;


public class Group_RootObject /// The Person Group object
{
    public string personGroupId { get; set; }
    public string name { get; set; }
    public object userData { get; set; }
}
public class Face_RootObject/// The Person Face object
{
    public string faceId { get; set; }
}
public class FacesToIdentify_RootObject /// Collection of faces that needs to be identified
{
    public string personGroupId { get; set; }
    public List<string> faceIds { get; set; }
    public int maxNumOfCandidatesReturned { get; set; }
    public double confidenceThreshold { get; set; }
}
public class Candidate_RootObject /// Collection of Candidates for the face
{
    public string faceId { get; set; }
    public List<Candidate> candidates { get; set; }
}
public class Candidate
{
    public string personId { get; set; }
    public double confidence { get; set; }
}
public class IdentifiedPerson_RootObject /// Name and Id of the identified Person
{
    public string personId { get; set; }
    public string name { get; set; }
}
class FaceAnalysis : MonoBehaviour
{

    public static FaceAnalysis Instance; /// Allows this class to behave like a singleton
    private TextMesh labelText; /// The analysis result text
    internal byte[] imageBytes;     /// Bytes of the image captured with camera
    internal string imagePath;    /// Path of the image captured with camera
    const string baseEndpoint = "https://westus.api.cognitive.microsoft.com/face/v1.0/"; /// Base endpoint of Face Recognition Service
    private const string key = "a3d51b5d46bf447c9711f4370fcf91c8"; /// Auth key of Face Recognition Service
    private const string personGroupId = "01";  /// Id (name) of the created person group 
    private void Awake() /// Initialises this class
    {
        Instance = this;// Allows this instance to behave like a singleton   
        gameObject.AddComponent<ImageCapture>();// Add the ImageCapture Class to this Game Object
        CreateLabel();// Create the text label in the scene
    }
    private void CreateLabel()/// Spawns cursor for the Main Camera
    {
        
        GameObject newLabel = new GameObject(); // Create a sphere as new cursor
        newLabel.transform.parent = gameObject.transform;// Attach the label to the Main Camera
        newLabel.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);// Resize and position the new cursor
        newLabel.transform.position = new Vector3(0f, 3f, 60f);
        labelText = newLabel.AddComponent<TextMesh>();// Creating the text of the Label
        
        labelText.anchor = TextAnchor.MiddleCenter;
        labelText.alignment = TextAlignment.Center;
        labelText.tabSize = 4;
        labelText.fontSize = 50;
        labelText.text = ".";
    }
    internal IEnumerator DetectFacesFromImage()/// Detect faces from a submitted image
    {
        WWWForm webForm = new WWWForm();
        string detectFacesEndpoint = $"{baseEndpoint}detect";
        imageBytes = GetImageAsByteArray(imagePath);  // Change the image into a bytes array
        using (UnityWebRequest www = UnityWebRequest.Post(detectFacesEndpoint, webForm))
        {
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", key);
            www.SetRequestHeader("Content-Type", "application/octet-stream");
            www.uploadHandler.contentType = "application/octet-stream";
            www.uploadHandler = new UploadHandlerRaw(imageBytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;
            Face_RootObject[] face_RootObject = JsonConvert.DeserializeObject<Face_RootObject[]>(jsonResponse);
            List<string> facesIdList = new List<string>();
            foreach (Face_RootObject faceRO in face_RootObject) // Create a list with the face Ids of faces detected in image
            {
                facesIdList.Add(faceRO.faceId);
                Debug.Log($"Detected face - Id: {faceRO.faceId}");
            }
            StartCoroutine(IdentifyFaces(facesIdList));
        }
    }
    static byte[] GetImageAsByteArray(string imageFilePath)/// Returns the contents of the specified file as a byte array.
    {
        FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        return binaryReader.ReadBytes((int)fileStream.Length);
    }
    internal IEnumerator IdentifyFaces(List<string> listOfFacesIdToIdentify)/// Identify the faces found in the image within the person group
    {
        
        FacesToIdentify_RootObject facesToIdentify = new FacesToIdentify_RootObject(); // Create the object hosting the faces to identify
        facesToIdentify.faceIds = new List<string>();
        facesToIdentify.personGroupId = personGroupId;
        foreach (string facesId in listOfFacesIdToIdentify)
        {
            facesToIdentify.faceIds.Add(facesId);
        }
        facesToIdentify.maxNumOfCandidatesReturned = 1;
        facesToIdentify.confidenceThreshold = 0.5;
        string facesToIdentifyJson = JsonConvert.SerializeObject(facesToIdentify); // Serialise to Json format
        byte[] facesData = Encoding.UTF8.GetBytes(facesToIdentifyJson);// Change the object into a bytes array
        WWWForm webForm = new WWWForm();
        string detectFacesEndpoint = $"{baseEndpoint}identify";
        using (UnityWebRequest www = UnityWebRequest.Post(detectFacesEndpoint, webForm))
        {
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", key);
            www.SetRequestHeader("Content-Type", "application/json");
            www.uploadHandler.contentType = "application/json";
            www.uploadHandler = new UploadHandlerRaw(facesData);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            string jsonResponse = www.downloadHandler.text;
            Debug.Log($"Get Person - jsonResponse: {jsonResponse}");
            Candidate_RootObject[] candidate_RootObject = JsonConvert.DeserializeObject<Candidate_RootObject[]>(jsonResponse);

            // For each face to identify that ahs been submitted, display its candidate
            foreach (Candidate_RootObject candidateRO in candidate_RootObject)
            {
                StartCoroutine(GetPerson(candidateRO.candidates[0].personId));

                // Delay the next "GetPerson" call, so all faces candidate are displayed properly
                yield return new WaitForSeconds(3);
            }
        }
    }

    /// <summary>
    /// Provided a personId, retrieve the person name associated with it
    /// </summary>
    internal IEnumerator GetPerson(string personId)
    {
        string getGroupEndpoint = $"{baseEndpoint}persongroups/{personGroupId}/persons/{personId}?";
        WWWForm webForm = new WWWForm();

        using (UnityWebRequest www = UnityWebRequest.Get(getGroupEndpoint))
        {
            www.SetRequestHeader("Ocp-Apim-Subscription-Key", key);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();
            string jsonResponse = www.downloadHandler.text;

            Debug.Log($"Get Person - jsonResponse: {jsonResponse}");
            IdentifiedPerson_RootObject identifiedPerson_RootObject = JsonConvert.DeserializeObject<IdentifiedPerson_RootObject>(jsonResponse);

            // Display the name of the person in the UI
            labelText.text = identifiedPerson_RootObject.name;
        }
    }

}