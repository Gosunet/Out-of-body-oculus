using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml;
using System.IO;
using ManageLog;

public class InitSceneHumanoids : MonoBehaviour
{

    public GameObject _humanoidRight;
    public GameObject _humanoidLeft;

    // doors counter on the scene.
    [SerializeField]
    private GameObject _text;

    [SerializeField]
    private GameObject _pilier;
    private float offset;

    // Define the type of the exercice
    private string _typeExercice = String.Empty;
    private string TypeExercice
    {
        get
        {
            return _typeExercice;
        }
        set
        {
            if (value != _typeExercice)
            {
                _typeExercice = value;
                _leftObject.SetActive(true);
                _rightObject.SetActive(true);
            }
        }
    }

    // Game object to move
    private GameObject _leftObject
    {
        get
        {
            if (_typeExercice == Utils.TYPE_HUMANOIDE)
            {
                _pilier.SetActive(false);
                _humanoidLeft.SetActive(true);
                return _humanoidLeft;
            }
            else
            {
                _humanoidLeft.SetActive(false);
                return (GameObject)_pilier.transform.FindChild("left_cylinder").gameObject;
            }
        }
    }
    private GameObject _rightObject
    {
        get
        {
            if (_typeExercice == Utils.TYPE_HUMANOIDE)
            {
                _pilier.SetActive(false);
                _humanoidRight.SetActive(true);
                return _humanoidRight;
            }
            else
            {
                _humanoidRight.SetActive(false);
                return (GameObject)_pilier.transform.FindChild("right_cylinder").gameObject;
            }
        }
    }

    private const float _minDistance = 4;
    private const float _maxDistance = 50; 

    // Value got from the socket
    private int _nbRepetition;
    private int _nbDistance;
    private int _percentageDiff;
    private float _scale;

    // Value of initial position of each humanoid
    private Vector3 _posHumanoidRight;
    private Vector3 _posHumanoidLeft;

    // Attribute to know the time response of the patient
    private System.DateTime _time;

    // Management of the distance between the humanoids
    private int _nbTests = 0;

    private int _distanceIndex;
    private int _randomDistance;

    private List<float> _listRangeDistance = new List<float>();
    private List<float> _listRandomDistance = new List<float>();
    private List<float> _listRangeDistanceCopy = new List<float>();
    private List<double> _listTime = new List<double>();

    private float _distanceMinimal;
    private float _distanceMaximal;

    // Number of tests achived
    private int _nbAnswers = 0;

    // Chosen model by the patient
    private float[] _modelSrcValues;
    // Chosen model by the psychologue
    private float[] _modelDstValues;
    // difference of morphology
    private float[] _differenceModels;

    // Variables for results 
    private bool _next = false;
    private bool _stop = false;
    // results file.
    private string _fileName;
    private XmlDocument _xmlModel;

    private List<bool> _answers = new List<bool>();

    string SEPARATOR = "\t";

    // Use this for initialization
    void Start()
    {
        //  initialize the paramaters received from the socket
        initParameters();

        // Initialize the models 
        initModels();

        // Initialize the range of distance
        initDistances();

        // Apply the distance scale 
        applyScaleDistance();
    }

    private void initParameters()
    {
        string resSocket = PlayerPrefs.GetString(Utils.PREFS_PARAM_HUMANOID);

        string[] parameters = resSocket.Split('_');

        // Parameters receivied from the socket
        _nbRepetition = int.Parse(parameters[0]);
        _nbDistance = int.Parse(parameters[1]);
        _nbTests = _nbDistance;
        _distanceMinimal = int.Parse(parameters[2]) / 10;
        if (_distanceMinimal < _minDistance) _distanceMinimal = _minDistance;

        _distanceMaximal = int.Parse(parameters[3]) / 10;
        if (_distanceMaximal > _maxDistance) _distanceMaximal = _maxDistance;

        TypeExercice = parameters[4].ToString();
        string scale = parameters[5];
        if(scale == "null" | scale == "0")
        {
            _scale = 1;
        }
        else
        {
            _scale = float.Parse(parameters[5]);
        }

    }

    private void initModels()
    {
        Vector3 humanoidBoxSize = _humanoidRight.GetComponent<BoxCollider>().size;

        // Offset entre le centre de l'humanoide et le pied pour la distance entre les 2 humanoides 
        offset = humanoidBoxSize.x * _humanoidRight.transform.localScale.x / 2;

        if (_typeExercice == Utils.TYPE_PILIER)
        {
            _leftObject.transform.localScale = new Vector3(8 * humanoidBoxSize.x *  _scale, 8 * humanoidBoxSize.y, 4 * humanoidBoxSize.z * _scale);
            _rightObject.transform.localScale = new Vector3(8 * humanoidBoxSize.x * _scale, 8 * humanoidBoxSize.y, 4 * humanoidBoxSize.z * _scale);
        }
        else
        {
            // Scale
            _humanoidRight.transform.localScale = new Vector3(8* _scale, 8, 8* _scale);

            // Rotation des bras 
            _humanoidRight.transform.FindChild("python/Hips/Spine/Spine1/Spine2/Spine3/RightShoulder/RightShoulderExtra").transform.localRotation = Quaternion.Euler(-58f, -32f, 39.2f);
            _humanoidRight.transform.FindChild("python/Hips/Spine/Spine1/Spine2/Spine3/LeftShoulder/LeftShoulderExtra").transform.localRotation = Quaternion.Euler(-58f, 32f, -39.2f);

            // --------- Initilisation de l'humanoide de gauche        
            // Scale
            _humanoidLeft.transform.localScale = new Vector3(8* _scale, 8, 8* _scale);

            // Rotation des bras 
            _humanoidLeft.transform.FindChild("python/Hips/Spine/Spine1/Spine2/Spine3/RightShoulder/RightShoulderExtra").transform.localRotation = Quaternion.Euler(-98.2f, -55f, 39.2f);
            _humanoidLeft.transform.FindChild("python/Hips/Spine/Spine1/Spine2/Spine3/LeftShoulder/LeftShoulderExtra").transform.localRotation = Quaternion.Euler(-98, 55f, -39.2f);
        }
        // multiplier l'offset par le scale 
        offset = offset * _scale;

        _leftObject.transform.localPosition = new Vector3(-4, 0, 2);
        _rightObject.transform.localPosition = new Vector3(4, 0, 2);
    }

    private void initDistances()
    {
        System.Random rnd = new System.Random();

        if (_nbDistance > 0)
        {
            List<float> listDistance = new List<float>();

            // Définition du pas
            float step = (float)((_distanceMaximal - _distanceMinimal) / _nbDistance);

            // initialiser une liste à pas fixe
            for (int i = 0; i < _nbDistance; i++)
            {
                float value = (float)(step * i + _distanceMinimal);
                listDistance.Add(value);
            }

            // Création de la liste de répétition 
            for (int i = 0; i < _nbRepetition; i++)
            {
                _listRangeDistance.AddRange(listDistance);
                _listRangeDistanceCopy.AddRange(listDistance);
            }

            _nbTests = _listRangeDistance.Count;
        }
    }

    private void applyScaleDistance()
    {
        if (_listRangeDistance.Count > 0)
        {
            _distanceIndex = UnityEngine.Random.Range(0, _listRangeDistance.Count);

            float newPosX = (float)(_listRangeDistance[_distanceIndex] / 2.0);

            // Définition de la nouvelle position des humanoides 
            _leftObject.transform.localPosition = new Vector3(-newPosX - offset, _leftObject.transform.localPosition.y, _leftObject.transform.localPosition.z);
            _rightObject.transform.localPosition = new Vector3(newPosX + offset, _rightObject.transform.localPosition.y, _rightObject.transform.localPosition.z);

            _listRangeDistance.RemoveAt(_distanceIndex);
            _nbDistance--;
            _nbAnswers++;

            _text.GetComponent<Text>().text = _nbAnswers.ToString() + "/" + _nbTests.ToString();
            _time = DateTime.Now;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (!_stop)
        {
            if (Input.GetKeyDown(KeyCode.O) || Input.GetMouseButtonDown(0))
            {
                Reponse(true);
                _next = true;
            }
            else if (Input.GetKeyDown(KeyCode.N) || Input.GetMouseButtonDown(1))
            {
                Reponse(false);
                _next = true;
            }

            if (_next)
            {
                //ajout du temps de reponse
                _listTime.Add((DateTime.Now - _time).TotalMilliseconds);

                if (_listRangeDistance.Count > 0)
                {
                    applyScaleDistance();
                    _next = false;
                }
                else
                {
                    _stop = true;


                    string directory = PlayerPrefs.GetString(Utils.PREFS_EXPERIMENT_PATH_FOLDER);
                    if (!string.Empty.Equals(directory))
                    {
                        string username = directory.Remove(0, directory.LastIndexOf('\\') + 1).Split('_')[0];

                        int numeroEx = recupNumeroExecice();
                        FileLog fl = new FileLog();
                        fl.createConfigFile(numeroEx);

                        Debug.Log("listerangeDistance = " + _listRangeDistanceCopy);
                        Debug.Log("listerangeDistance.Count = " + _listRangeDistanceCopy.Count);

                        fl.createResultFileHumanoide(directory, username, numeroEx, _listRangeDistanceCopy, _listTime, _answers);

                    }



                    SocketClient.GetInstance().Write(Utils.SOCKET_END_HUMANOID);  // Send message "end of exercice" to the server
                    Utils.CurrentState = State.WAITING;
                }
            }
        }
    }

    void Reponse(bool rep)
    {
        _answers.Add(rep);
        _next = true;
    }

    int recupNumeroExecice()
    {
        int n = PlayerPrefs.GetInt(Utils.PREFS_NUMERO_EXERCICE);
        PlayerPrefs.SetInt(Utils.PREFS_NUMERO_EXERCICE, n + 1);
        return n;
    }

}
