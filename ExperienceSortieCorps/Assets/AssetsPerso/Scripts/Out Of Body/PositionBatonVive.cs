using UnityEngine;
using System.Collections;

public class PositionBatonVive : MonoBehaviour {

	[SerializeField]
	private GameObject _avatarPosition;
    [SerializeField]
    private GameObject _baton;

    private GameObject _avatar;
    // Vector used to calculate hand's position
    private Vector3 m_initialPosition;
	// temporary position to update hand position
	private Vector3 _tmpPosition;

    private Vector3 controllerPosition;

    public bool triggerButtonDown = false;
    private bool init = false;

    private SteamVR_TrackedController device;
    void Start () {


        device = GetComponent<SteamVR_TrackedController>();
        device.TriggerClicked += Trigger;
        GetInitialPositionAvatar();

        _tmpPosition = m_initialPosition;

    }

    void Trigger(object sender, ClickedEventArgs e)
    {
        // Trigger ne fonctionne pas 
        _avatarPosition = _avatarPosition.transform.FindChild(PlayerPrefs.GetString(Utils.PREFS_MODEL).Split(';')[0].Split('/')[2]).gameObject;

        GetInitialPositionAvatar();
        _tmpPosition = m_initialPosition - device.transform.localPosition;    
    }

    // Update is called once per frame
    void Update () {
        Debug.Log("position device : " + device.transform.localPosition);
        Vector3 newPosBaton = _tmpPosition;// + device.transform.localPosition;

        newPosBaton.y += device.transform.localPosition.y*10 - 10f;
        newPosBaton.x += device.transform.localPosition.x*10 + 5f;

        _baton.transform.localPosition = newPosBaton;
        _baton.transform.localRotation = device.transform.localRotation;

        Debug.Log("position baton après update " + _baton.transform.localPosition);
    }


    /// <summary>
    /// Gets the initial world position of the avatar.
    /// </summary>
    void GetInitialPositionAvatar()
	{
        m_initialPosition = _avatarPosition.transform.localPosition;
        
        m_initialPosition.y += 8f;
		m_initialPosition.z -= 3f;
	}
    /// <summary>
    /// Gets the initial world position of the controlleur.
    /// </summary>
    void GetDevicePosition()
    {
        Vector3 pos = device.gameObject.transform.position;
        controllerPosition = new Vector3(pos.x, pos.y, pos.z);
    }

}
