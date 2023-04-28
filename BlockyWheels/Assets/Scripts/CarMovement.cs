using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Steamworks;
using Mirror;

public class CarMovement : NetworkBehaviour
{
    public static CarMovement instance;

    [Header("Networking")]
    [SyncVar] public int connectionID;
    [SyncVar] public int playerIDNumber;
    [SyncVar] public ulong playerSteamID;
    [SyncVar] public bool finished;
    [SyncVar] public bool kicked;

    public Vector2 movement;

    public int index;
    [SyncVar(hook = nameof(ChangeName))] public string playerName;
    [SyncVar(hook = nameof(PlayerReady))] public bool ready;
    [SyncVar(hook = nameof(GameStart))] public bool start;

    public CameraManager localCamera;
    private MyNetworkManager networkManager;

    MyNetworkManager NetworkManager
    {
        get
        {
            if (networkManager != null) return networkManager;
            return networkManager = MyNetworkManager.singleton as MyNetworkManager;
        }
    }


    [Header("Car properties")]
    public float speed;
    public float accelerationSpeed;
    public float turnSpeed;
    public bool canAccelerate;
    public bool canTurn = true;
    public float interpolationPower;
    public Vector2 minMaxSpeed;
    public Vector2 baseMinMax;

    [Header("Different cars")]
    public int selectedCar;
    public CarScriptableObject[] carVariants;
    public float bonusSpeed = 0;
    public float bonusAccelerationSpeed = 0;
    public float bonusSpeedLoss = 0;
    public float bonusAccelerationDelay = 0;

    [Header("Crashing stuff")]
    [Tooltip("Min speed to have so you don't die")]
    public float deathSpeed;
    [Tooltip("Max obstacles you can hit before dying")]
    public int obstacleHits;
    public bool dead;
    public bool invincible;

    [Header("Other")]
    public BoxCollider coreCollider;
    public BoxCollider triggerCollider;
    public int currentLane;
    public bool reversed = false;
    public Transform statsPanel;
    public Bomb bomb;

    [Header("Effects")]
    public ParticleSystem speedEffect;
    public ParticleSystem explosion;
    public ParticleSystem smoke;
    public GameObject shield;
    public GameObject stunned;
    public GameObject smokePrefab;
    public bool controlsSpeedEffect = true;

    [Header("Events")]
    public int forcedLane = 0;
    public GameObject[] roadObstacles;
    public GameObject wrongLanePrefab;
    public GameObject missilePrefab;

    [Header("Nitro")]
    public float nitro;
    public float nitroAcceleration;
    public float nitroBurn;
    public float nitroSpeed;
    public bool inNitro;
    public ParticleSystem nitroParticle;

    [Header("Lane bounds")]
    public Vector2 currentLaneBounds;
    public Vector2 correctLaneBounds;
    public Vector2 wrongLaneBounds;

    [Header("Other")]
    public LayerMask groundMask;

    public bool pickedUpPower;
    PlayerControls controls;
    private Transform cam;
    [HideInInspector]
    public Rigidbody rb;
    public GameObject gfx;
    public Text carNameText;
    private Transform carCanvas;
    private int indexCar = 0;
    private int lastDirection = -10;

    private void Awake()
    {
        controls = new PlayerControls();

        DontDestroyOnLoad(gameObject);

        if (SaveLoadManager.IntToBool(PlayerPrefs.GetInt(SaveLoadManager.invertedControlsString)))
            controls.Gameplay.Move.performed += ctx => movement = ctx.ReadValue<Vector2>();
        else controls.Gameplay.Move.performed += ctx => movement = new Vector3(ctx.ReadValue<Vector2>().y, -ctx.ReadValue<Vector2>().x);
        controls.Gameplay.Reset.performed += ctx => GameManager.instance.ResetLevel();
        controls.Gameplay.SpectateUp.performed += ctx => localCamera.ChangeSpectate(1); 
        controls.Gameplay.SpectateDown.performed += ctx => localCamera.ChangeSpectate(-1);
    }

    void Start()
    {
        canTurn = false;
        canAccelerate = false;
        rb = GetComponent<Rigidbody>();
        cam = Camera.main.transform;
        if (hasAuthority && isLocalPlayer)
            carNameText.transform.parent.gameObject.SetActive(true);
        carNameText.text = playerName;
        carCanvas = carNameText.transform.parent;
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    #region Networking

    public override void OnStartAuthority()
    {
        if (PlayerPrefs.HasKey(SaveLoadManager.selectedCar)) selectedCar = PlayerPrefs.GetInt(SaveLoadManager.selectedCar);
        else selectedCar = 0;

        SetPlayerCar(selectedCar);
        ChangeCar(selectedCar);
        gameObject.name = "LocalCar";
        instance = this;
        if (GameManager.instance != null) GameManager.instance.localPlayer = this;

        if (NetworkManager.onlineScene.Contains("Lobby"))
        {
            CmdSetPlayerName(SteamFriends.GetPersonaName());
            LobbyManager.instance.FindLocalPlayer();
            LobbyManager.instance.UpdateLobbyName();
        }

        minMaxSpeed = new Vector2(500, 2000 + bonusSpeed);
        baseMinMax = minMaxSpeed;
    }

    public override void OnStartClient()
    {
        NetworkManager.players.Add(this);

        if (NetworkManager.onlineScene.Contains("Lobby"))
        {
            LobbyManager.instance.UpdateLobbyName();
            LobbyManager.instance.UpdatePlayerList();
        }
    }

    public override void OnStopClient()
    {
        NetworkManager.players.Remove(this);

        if (NetworkManager.onlineScene.Contains("Lobby"))
        {
            LobbyManager.instance.UpdatePlayerList();
        }
        else NetworkManager.StopHost();
    }

    [Command]
    private void CmdSetPlayerName(string playerName)
    {
        this.ChangeName(this.playerName, playerName);
    }

    private void PlayerReady(bool oldValue, bool newValue) // Used to update syncvar
    {
        if (isServer) this.ready = newValue;

        if (isClient)
        {
            LobbyManager.instance.UpdateClient();
        }
    }

    private void GameStart(bool oldValue, bool newValue)
    {
        if (isServer) this.start = newValue;

        if (start) FadePanel.instance.StartCoroutine(FadePanel.instance.FadeIn());
    }

    [Command]
    private void SetGameStart()
    {
        this.GameStart(this.start, !this.start);
    }

    public void ChangeStart()
    {
        if (hasAuthority) SetGameStart();
    }

    [Command]
    private void SetPlayerReady() // Run on the server
    {
        this.PlayerReady(this.ready, !this.ready);
    }

    public void ChangeReady() // Run on this client
    {
        if (hasAuthority) SetPlayerReady();
    }

    [Command]
    private void SetPlayerCar(int car)
    {
        this.ChangeCar(car);
    }

    [Command]
    public void DestroyServerObject(GameObject obj)
    {
        DestroyClientObject(obj);
    }

    [ClientRpc]
    public void DestroyClientObject(GameObject obj)
    {
        Destroy(obj);
    }

    private void ChangeName(string oldName, string newName)
    {
        if (isServer) this.playerName = newName;
        if (isClient)
        {
            LobbyManager.instance.UpdatePlayerList();
            carNameText.text = playerName;
        }
    }

    // Prepare for race

    [Command]
    public void CmdPrepare()
    {
        RpcPrepare();
    }

    [ClientRpc]
    public void RpcPrepare()
    {
        PrepareForRace();
    }

    // Shield Power Up

    [Command(requiresAuthority = false)]
    public void CmdShield(bool shielded)
    {
        RpcShield(shielded);
    }

    [ClientRpc]
    public void RpcShield(bool shielded)
    {
        invincible = shielded;
        shield.SetActive(shielded);
    }

    // Nitro Power Up

    [Command(requiresAuthority = false)]
    public void CmdNitro(bool apply)
    {
        RpcNitro(apply);
    }

    [ClientRpc]
    public void RpcNitro(bool apply)
    {
        inNitro = apply;

        if (!apply) nitroParticle.Stop();
        else nitroParticle.Play();
    }

    // Reverse Power up

    [Command(requiresAuthority = false)]
    public void Reverse(bool reverse)
    {
        ReverseClient(reverse);
    }

    [ClientRpc]
    public void ReverseClient(bool reverse)
    {
        reversed = reverse;
        stunned.SetActive(reverse);
        if (reverse) stunned.transform.SetParent(null);
        else stunned.transform.SetParent(transform);
    }

    // Bomb Power up

    [Command(requiresAuthority = false)]
    public void CmdBomb(CarMovement carBombed, float timer, bool bombed, bool pass)
    {
        RpcBomb(carBombed, timer, bombed, pass);
    }

    [ClientRpc]
    public void RpcBomb(CarMovement carBombed, float timer, bool bombed, bool pass)
    {
        carBombed.bomb.gameObject.SetActive(bombed);
        if (bombed)
        {
            carBombed.bomb.passDelay = 1;
            carBombed.bomb.Activate(timer);
            carBombed.bomb.passable = pass;
        }
    }

    public void ChangeLaneNetwork(float delay, int direction)
    {
        if (isServer) RpcChangeLane(delay, direction);
        else CmdChangeLane(delay, direction);
    }

    [Command]
    public void CmdChangeLane(float delay, int direction)
    {
        RpcChangeLane(delay, direction);
    }

    [ClientRpc]
    public void RpcChangeLane(float delay, int direction)
    {
        StartCoroutine(ChangeLane(delay, direction));
    }

    public IEnumerator ChangeLane(float delay, int direction)
    {
        if (!isLocalPlayer) yield break;

        if (direction == lastDirection) yield break;

        lastDirection = direction;

        if (delay > 0) yield return new WaitForSeconds(delay);

        // Update bounds
        localCamera.transform.SetParent(transform);
        currentLaneBounds = new Vector2(correctLaneBounds.x, wrongLaneBounds.y);

        // Move
        CmdSpawnSmoke();
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + 31.25f * direction);
        CmdSpawnSmoke();

        // Update bounds
        if (direction < 0) currentLaneBounds = correctLaneBounds;
        else currentLaneBounds = wrongLaneBounds;

        localCamera.transform.SetParent(null);

        yield return new WaitForSeconds(7);

        if (direction > 0) StartCoroutine(ChangeLane(3, -1));
    }

    [Command(requiresAuthority = false)]
    public void CmdSpawnSmoke()
    {
        GameObject smoke = Instantiate(smokePrefab, transform.position, Quaternion.identity);
        smoke.transform.localScale = Vector3.one * 3;
        NetworkServer.Spawn(smoke);
    }

    public void PrepareForRace()
    {
        if (!hasAuthority) return;
        transform.position = new Vector3(46, .5f, GameManager.instance.carLanes[playerIDNumber - 1]);
        transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false;
        minMaxSpeed = new Vector2(500, 2000 + bonusSpeed);
        speed = 800;
    }

    public void ActivateCamera(bool active)
    {
        if (!hasAuthority) return;

        if (active)
        {
            if (localCamera.cam == null) localCamera.cam = localCamera.GetComponent<Camera>();
            localCamera.cam.fieldOfView = 70;
            localCamera.transform.SetParent(null);
            localCamera.transform.position = new Vector3(0, 13, 20);
            localCamera.transform.rotation = Quaternion.Euler(30, 180, 0);
            localCamera.gameObject.SetActive(true);
        }
        else
        {
            localCamera.transform.SetParent(transform);
            localCamera.gameObject.SetActive(false);
        }
    }

    public void LeaveLobby()
    {
        if (!hasAuthority) return;

        Destroy(localCamera.gameObject);
        Destroy(carCanvas.gameObject);

        if (isServer) { NetworkManager.StopHost(); }
        else if (isClient) NetworkManager.StopClient();

        MyNetworkManager.multiplayer = false;
        NetworkServer.Shutdown();
    }

    #endregion

    public void SendPrediction(ObstacleLaunch obstacleInputState)
    {
        NetworkClient.Send(obstacleInputState);
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if (FadePanel.instance == null) FadePanel.instance = FindObjectOfType<FadePanel>();

        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            LeaveLobby();
            return;
        }

        if (SceneManager.GetActiveScene().name == "CampaignScene") return;

        if (hasAuthority)
        {
            Invoke("Wait", .2f);
        }
    }

    void Wait()
    {
        if (isServer) RpcPrepare();
        else  CmdPrepare();
    }

    void Update()
    {
        if (!hasAuthority || !isLocalPlayer) return;

        if (kicked) LeaveLobby();

        if (inNitro)
        {
            // If nitro runs out, cancel it
            if (nitro <= 0) { CmdNitro(false); }

            // Decrease nitro
            nitro -= nitroBurn * Time.deltaTime;

            // If maxSpeed hasn't reached nitro maxSpeed - increase it by nitroAcceleration
            if (minMaxSpeed.y < nitroSpeed)
                minMaxSpeed = new Vector2(minMaxSpeed.x, minMaxSpeed.y + nitroAcceleration);

            // Do the same with the speed
            if (speed < nitroSpeed) speed += nitroAcceleration;
        }
        else
        {
            // If max speed is different than base max speed
            if (minMaxSpeed.y > baseMinMax.y)
            {
                // If you are going slower than max speed
                if (speed < minMaxSpeed.y)
                {
                    // Update max speed to current speed
                    minMaxSpeed = new Vector2(minMaxSpeed.x, speed);
                    if (minMaxSpeed.y < 2000) minMaxSpeed = new Vector2(minMaxSpeed.x, 2000 + bonusSpeed);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (stunned.activeInHierarchy)
        {
            stunned.transform.position = transform.position;
            stunned.transform.Rotate(Vector3.up, 5);
        }

        if (hasAuthority && isLocalPlayer)
        {
            if (dead) return;

            // Clamp within camera view || Mathf.Clamp(transform.position.x, cam.position.x - 22.5f, cam.position.x + 32.5f) - clamp in camera view
            if (!finished && CameraManager.instance != null && CameraManager.instance.enabled)
                transform.position = new Vector3(transform.position.x, transform.position.y,
                    Mathf.Clamp(transform.position.z, currentLaneBounds.x, currentLaneBounds.y));

            carCanvas.transform.position = transform.position;
            rb.velocity = transform.right * speed * Time.fixedDeltaTime;

            // Rotation
            if (canTurn && movement.y != 0) // 150 - 210
            {
                if (!finished)
                {
                    float input = 0;

                    if (!reversed) input = -movement.y;
                    else input = movement.y;

                    if ((transform.rotation.eulerAngles.y > 220 && input > 0) ||
                    (transform.rotation.eulerAngles.y < 140 && input < 0)) return;

                    transform.rotation = Quaternion.Lerp(transform.rotation,
                        transform.rotation * Quaternion.Euler(new Vector3(0, input
                        * turnSpeed * rb.velocity.normalized.magnitude, 0)), .125f);
                }

                // 6.5 -0.25
                // Update Lane when turning
                if (transform.position.z > GameManager.instance.carLanes[0] - GameManager.instance.laneSize / 2) currentLane = 0;
                else
                {
                    for (int i = 1; i < GameManager.instance.carLanes.Length; i++)
                    {
                        if (transform.position.z > GameManager.instance.carLanes[i] - GameManager.instance.laneSize / 2 &&
                            transform.position.z < GameManager.instance.carLanes[i - 1] - GameManager.instance.laneSize / 2) currentLane = i;
                    }
                }
            }

            if (speedEffect != null) speedEffect.transform.position = new Vector3(transform.position.x - 35, speedEffect.transform.position.y, speedEffect.transform.position.z);
            else if (speedEffect == null && GameManager.instance != null) speedEffect = GameManager.instance.speedEffect;

            speed = Mathf.Clamp(speed, minMaxSpeed.x, minMaxSpeed.y);

            // Speed regulation
            if (movement.x != 0 && canAccelerate)
            {
                if (controlsSpeedEffect)
                {
                    if (speed > 1700 && !speedEffect.isPlaying) speedEffect.Play();
                    else if (speed < 1700 && speedEffect.isPlaying) speedEffect.Stop();
                }

                if (speed < 800 && movement.x < 0) return;

                speed += movement.x * accelerationSpeed;
            }
        }
    }

    public void ChangeCar(int index)
    {
        CarScriptableObject selectedCar = carVariants[index];

        // Change GFX
        Destroy(gfx);
        GameObject newGFX = Instantiate(selectedCar.GFX, transform);
        newGFX.transform.localPosition = Vector3.zero;
        newGFX.transform.localRotation = Quaternion.identity;
        gfx = newGFX;

        // Change collider
        coreCollider.center = selectedCar.colliderCenter;
        coreCollider.size = selectedCar.colliderDimensions;
        triggerCollider.center = selectedCar.colliderCenter;
        triggerCollider.size = selectedCar.colliderDimensions;

        // Change speed
        bonusSpeed = selectedCar.bonusMaxSpeed;
        minMaxSpeed = new Vector2(minMaxSpeed.x, minMaxSpeed.y + bonusSpeed);

        // Change acceleration speed
        bonusAccelerationSpeed = selectedCar.bonusAccelerationSpeed;
        accelerationSpeed = accelerationSpeed + bonusAccelerationSpeed;

        // Change speed loss on crash
        bonusSpeedLoss = selectedCar.bonusSpeedLoss;

        // Change acceleration delay after crash
        bonusAccelerationDelay = selectedCar.bonusAccelerationDelay;

        // Change shield
        shield.transform.localScale = Vector3.one * selectedCar.shieldScale;

        indexCar++;
    }

    public void ReduceSpeed(float speedReduce)
    {
        if (!hasAuthority || invincible) return;

        if (speed > deathSpeed && obstacleHits < 2) obstacleHits = 2;
        speed -= speedReduce + bonusSpeedLoss;
        canAccelerate = false;

        if (speedReduce > 600) Invoke("EnableAccelerate", 3 + bonusAccelerationDelay);
        else Invoke("EnableAccelerate", 1 + bonusAccelerationDelay);

        if (speed < deathSpeed)
        {
            obstacleHits--;
            if (obstacleHits == 0) // Die Death
            {
                dead = true;
                explosion.Play();
                rb.constraints = RigidbodyConstraints.None;

                rb.AddForce(Vector3.up * speed * Time.deltaTime, ForceMode.Impulse);
                rb.AddTorque(Vector3.forward * speed * 4 + (Vector3.right * speed * 2) * Time.deltaTime, ForceMode.Impulse);

                Invoke("Respawn", 2);
            }
        }
    }

    public void Respawn()
    {
        gameObject.layer = 13; // No collision with player
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = Vector2.zero;
        transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
        obstacleHits = 2;
        speed = 1000;
        transform.position = new Vector3(transform.position.x, .5f, GameManager.instance.carLanes[2]);
        dead = false;

        if (isServer) RpcInvincibility();
        else CmdInvincibility();
    }

    [Command]
    private void CmdInvincibility()
    {
        RpcInvincibility();
    }

    [ClientRpc]
    private void RpcInvincibility()
    {
        StartCoroutine(Invincibility());
    }

    public IEnumerator Invincibility()
    {
        invincible = true;
        rb.useGravity = false;
        triggerCollider.enabled = true;
        coreCollider.enabled = false;
        yield return new WaitForSeconds(.35f);
        gfx.SetActive(false);
        yield return new WaitForSeconds(.35f);
        gfx.SetActive(true);
        yield return new WaitForSeconds(.35f);
        gfx.SetActive(false);
        yield return new WaitForSeconds(.35f);
        gfx.SetActive(true);
        yield return new WaitForSeconds(.35f);
        gfx.SetActive(false);
        yield return new WaitForSeconds(.35f);
        gfx.SetActive(true);
        coreCollider.enabled = true;
        rb.useGravity = true;
        triggerCollider.enabled = false;
        invincible = false;
        gameObject.layer = 12; // Back to collision with player
    }

    public IEnumerator Straighten()
    {
        if (!hasAuthority) yield break;

        int yRotation = 0;
        if (transform.rotation.eulerAngles.y < 177) yRotation = 1;
        else yRotation = -1;

        while (transform.rotation.eulerAngles.y < 177 || transform.rotation.eulerAngles.y > 183)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, transform.rotation * Quaternion.Euler(new Vector3(0, yRotation * turnSpeed * rb.velocity.normalized.magnitude, 0)), .125f);
            yield return null;
        }
    }

    public IEnumerator PowerUpRemove(float duration)
    {
        if (!hasAuthority) yield break;

        yield return new WaitForSeconds(duration);

        if (invincible)
        {
            CmdShield(false);
        }
        else if (reversed)
        {
            Reverse(false);
        }

        pickedUpPower = false;
    }

    private void EnableAccelerate() // Invoked
    {
        if (!hasAuthority) return;

        canAccelerate = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasAuthority) return;

        if (other.GetComponent<Impact>()) ReduceSpeed(1000);

        if (other.gameObject.tag == "Finish") 
        {
            GameManager.instance.FinishLevel(this);
            CmdFinish(true);
            if (GameManager.instance.multiplayer) localCamera.ChangeSpectate(1);
            StartCoroutine(Finish());
            controlsSpeedEffect = false;
            speedEffect.Stop();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out CarMovement otherCar))
        {
            if (bomb.gameObject.activeInHierarchy && bomb.passable && bomb.passTimer <= 0 && bomb.passDelay <= 0)
            {
                CmdBomb(otherCar, bomb.timer, true, true);
                CmdBomb(this, 0, false, true);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdFinish(bool finish)
    {
        finished = finish;
    }

    public IEnumerator Finish()
    {
        int level = SceneManager.GetActiveScene().buildIndex;
        statsPanel = GameManager.instance.statsPanel;
        localCamera.StartCoroutine(localCamera.ChangeFOV(60));

        yield return new WaitForSeconds(1.5f);

        if (!GameManager.instance.multiplayer)
        {
            float roundedTime = Mathf.Round(GameManager.instance.time * 100f) / 100f;
            float bestTime = 0;

            if (PlayerPrefs.HasKey(SaveLoadManager.bestTimeStrings[level])) // Check if time is less than best time
            {
                bestTime = PlayerPrefs.GetFloat(SaveLoadManager.bestTimeStrings[level]);
                print("Best time: " + bestTime);
                if (bestTime < 5f || roundedTime < bestTime)
                {
                    PlayerPrefs.SetFloat(SaveLoadManager.bestTimeStrings[level], roundedTime);
                    bestTime = roundedTime;
                    print("invalid or less time than best - updating time to " + roundedTime);
                }
            }
            else
            {
                PlayerPrefs.SetFloat(SaveLoadManager.bestTimeStrings[level], roundedTime);
                bestTime = roundedTime;
                print("There is no key so setting best to current: " + roundedTime);
            }

            statsPanel.gameObject.SetActive(true);
            statsPanel.GetComponentsInChildren<Text>()[0].text = "Level " + SceneManager.GetActiveScene().buildIndex.ToString();
            statsPanel.GetComponentsInChildren<Text>()[1].text = "Best time: " + bestTime.ToString();
            statsPanel.GetComponentsInChildren<Text>()[2].text = "Time: " + roundedTime.ToString();

            if (PlayerPrefs.HasKey(SaveLoadManager.campaignLevelString))
            {
                if (PlayerPrefs.GetInt(SaveLoadManager.campaignLevelString) < level + 1) PlayerPrefs.SetInt((SaveLoadManager.campaignLevelString), level + 1);
            }
            else PlayerPrefs.SetInt((SaveLoadManager.campaignLevelString), level + 1);

            if (level == 15) statsPanel.GetComponentsInChildren<Button>()[1].gameObject.SetActive(false);

            while (statsPanel.localPosition.y < -5)
            {
                statsPanel.Translate(Vector3.up * 50);
                yield return null;
            }

            statsPanel.transform.localPosition = Vector3.zero;
        }
    }
}