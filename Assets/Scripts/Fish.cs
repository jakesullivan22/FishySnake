using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Fish : MonoBehaviour
{
    static GameManager gm;
    static Camera mainCamera;
    static Color startingColor;

    static float standardFishHeight; 
    static float maxScale = 1.73f;
    static float minScale = 0.64f;
    float scale;

    static float maxSpeed = 222;
    static float minSpeed = 50;
    float speed;

    [SerializeField] int points = 0;
    [SerializeField] float food;
    [SerializeField] float hp;

    public GameManager.moveMode moveMode = GameManager.moveMode.stop;
    [SerializeField] Vector2 moveDirection = Vector2.left;
    bool spawnedLeft;
    
    static GameManager.VoidFish onShock;
    bool immuneToShock;
    
    Image myImage;
    public GameObject eyebrow;

    void Start()
    {
        gm = GameManager.gm;
        mainCamera = Camera.main;
        if (GetComponent<SnakeBody>() == null)
        {
            myImage = transform.GetChild(0).GetComponent<Image>();
            startingColor = myImage.color;
            standardFishHeight = GetComponent<CapsuleCollider2D>().size.y;
        }
        
        SetRandomSpeed();
        RegisterOnShock(TurnAround);
        RegisterOnShock(TakeDamage);
        PlayerController.RegisterOnGrow(EyebrowCheck);
        PlayerController.RegisterOnFirstKeyPress(SetMoveMode);
        PlayerController.RegisterOnDoneEating(GetEaten);
    }

    void EyebrowCheck(float _float)
    {
        eyebrow.SetActive(standardFishHeight * scale > PlayerController.pc.GetTileDiameter()); //fish not angry or threatening looking if smaller than snake width
    }

    void Respawn()
    {
        //move fish
        float randY = Random.Range(.15f, .85f);
        spawnedLeft = Random.Range(0f, 1f) < .5f;
        Vector3 spawnCandidate = mainCamera.ViewportToScreenPoint(new Vector3(spawnedLeft ? -.3f : 1.3f, randY, 0f));
        moveDirection = spawnedLeft ? Vector2.right : Vector2.left;
        transform.position = spawnCandidate;

        //reset anything that could have changed
        moveMode = GameManager.moveMode.grid;
        immuneToShock = false;
        myImage.color = startingColor;
        CancelInvoke(); //shock immunity or respawn after death

        //scale fish
        Invoke("Rescale", Time.deltaTime);
    }

    void Rescale()
    {
        scale = Mathf.Pow(Random.Range(minScale, maxScale), 2) * 2f;
        scale = Mathf.Round(scale); //round scale to nearest .5
        scale /= 2f;
        transform.localScale = spawnedLeft ? FlipX(Vector3.one) : Vector3.one;
        transform.localScale = transform.localScale * scale;
        EyebrowCheck(1f);

        //set stats
        food = scale / Mathf.Pow(maxScale, 2);
        hp = Mathf.Pow(scale, 2) * 11f / 20f;
        points = (int)(scale * 2f);
        SetRandomSpeed();
    }

    void TurnOffShockImmunity()
    {
        immuneToShock = false;
        myImage.color = startingColor;
    }

    void GetEaten(Fish _fish)
    {
        if (_fish != this)
        {
            return;
        }

        if (GetComponent<SnakeBody>() != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Respawn();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (moveMode == GameManager.moveMode.stop)
        {
            return;
        }

        if (immuneToShock)
        {
            return;
        }

        if (collision.gameObject.GetComponent<SnakeBody>() != null)
        {
            if (hp > 0)
            {
                Instantiate(gm.lightningPrefab, collision.contacts[0].point, Quaternion.LookRotation( Vector3.forward, -collision.contacts[0].normal), GameCanvas.gc.transform);
                onShock(this);
            }
        }
    }

    void TurnAround(Fish _fish)
    {
        if (_fish != this)
        {
            return;
        }

        moveDirection *= -1f;
        transform.localScale = FlipX(transform.localScale);
    }

    Vector3 FlipX (Vector3 _vector)
    {
        return new Vector3(-_vector.x, _vector.y, _vector.z);
    }

    void TakeDamage(Fish _fish)
    {
        if (_fish != this)
        {
            return;
        }

        hp--;
        myImage.color = Color.red;
        immuneToShock = true;
        Invoke("TurnOffShockImmunity", .1f);

        if (hp <= 0)
        {
            moveMode = GameManager.moveMode.stop;
            Vector3 vScale = transform.localScale;
            transform.localScale = new Vector3(vScale.x, -vScale.y, vScale.z);
            Invoke("Respawn", 10f);
        }
    }

    void SetRandomSpeed()
    {
        speed = Random.Range(minSpeed, maxSpeed);
    }

    void SetMoveMode(GameManager.moveMode _moveMode)
    {
        moveMode = _moveMode;
    }

    public int GetPoints()
    {
        return points;
    }

    public float GetFood()
    {
        return food;
    }

    public float GetScale()
    {
        return scale;
    }

    public float GetHP()
    {
        return hp;
    }

    public static float GetStandardFishHeight()
    {
        return standardFishHeight;
    }

    void OnDisable()
    {
        PlayerController.UnregisterOnDoneEating(GetEaten);
        PlayerController.UnregisterOnGrow(EyebrowCheck);
        PlayerController.UnregisterOnFirstKeyPress(SetMoveMode);
        UnregisterOnShock(TurnAround);
        UnregisterOnShock(TakeDamage);
    }

    public static void RegisterOnShock(GameManager.VoidFish _funcToAdd)
    {
        onShock += _funcToAdd;
    }

    public static void UnregisterOnShock(GameManager.VoidFish _funcToRemove)
    {
        onShock -= _funcToRemove;
    }

    private void Update()
    {
        switch (moveMode)
        {
            case GameManager.moveMode.grid:
                transform.Translate(moveDirection * Time.deltaTime * speed, Space.World);
                break;
            default:
                return;
        }

        //if out of bounds, then respawn
        if (Mathf.Abs(mainCamera.ScreenToViewportPoint(transform.position).x - .5f) > .8f)
        {
            Respawn();
        }
    }
}
