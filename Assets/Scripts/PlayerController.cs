using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController pc;

    GameManager gm;
    GameCanvas gc;

    public GameObject prefabBody;
    List<SnakeBody> bodyparts = new List<SnakeBody>();
    SnakeBody tail;

    int tailLength = 3;
    int minLengthToEatSelf = 14;
    float pctFedUntilLengthen = 1f;
    Coroutine eatSelf;

    RectTransform rect;
    float tileDiameter;
    float startingTileDiameter;

    float speed = 200f;
    float maxSpeed = 250f;
    float targetSpeed;

    Vector2 moveDirection;
    Vector2 desiredDirection;
    GameManager.moveMode moveMode;

    Dictionary<KeyCode, Vector2> controls = new Dictionary<KeyCode, Vector2>();
    static GameManager.VoidNull onLengthen;
    static GameManager.VoidNull onSetTail;
    static GameManager.VoidNull onDie;
    static GameManager.VoidFish onEat;
    static GameManager.VoidFish onDoneEating;
    static GameManager.VoidFloat onGrow;
    static GameManager.VoidVector4 onTurn;
    static GameManager.VoidMoveMode onSetMoveMode;
    static GameManager.VoidMoveMode onFirstKeyPress;

    bool isInvincible;

    void Awake()
    {
        pc = this;
        targetSpeed = speed;
        rect = GetComponent<RectTransform>();
        tileDiameter = rect.rect.height;
        startingTileDiameter = tileDiameter;
        RegisterOnGrow(Grow);
    }

    private void Start()
    {
        gm = GameManager.gm;
        gc = GameCanvas.gc;

        controls.Add(KeyCode.D, Vector2.right);
        controls.Add(KeyCode.RightArrow, Vector2.right);
        controls.Add(KeyCode.W, Vector2.up);
        controls.Add(KeyCode.UpArrow, Vector2.up);
        controls.Add(KeyCode.S, Vector2.down);
        controls.Add(KeyCode.DownArrow, Vector2.down);
        controls.Add(KeyCode.A, Vector2.left);
        controls.Add(KeyCode.LeftArrow, Vector2.left);

        RegisterOnLengthen(Lengthen);
        RegisterOnLengthen(TailIsEdibleCheck);
        RegisterOnDie(StopMovement);
        RegisterOnFirstKeyPress(SetMoveMode);

        moveMode = GameManager.moveMode.stop;

        for (int i = 1; i < transform.parent.childCount; i++)
        {
            bodyparts.Add(transform.parent.GetChild(i).GetComponent<SnakeBody>());
        }
        SetTail();

        StartCoroutine(WaitForFirstKeyPress());
    }

    void ChangeDirection(Vector2 _direction)
    {
        Vector2 backwardsCheck = -moveDirection;
        if (_direction == backwardsCheck || _direction == moveDirection) //can't go backwards or go in the current direction
        {
            return;
        }

        //make sure the head is far enough past the neck before turning
        if (moveMode == GameManager.moveMode.grid && Vector3.Dot(moveDirection, transform.position - bodyparts[1].transform.position) < tileDiameter)
        {
            return;
        }

        Vector4 pointDirection = new Vector4(transform.position.x, transform.position.y, _direction.x, _direction.y);

        onTurn(pointDirection); //tell body that the head turned at this point and at this direction
        moveDirection = _direction; //the head will start moving in the new direction
        gm.RotateRect(rect, _direction);
    }

    public GameManager.moveMode GetMoveMode()
    {
        return moveMode;
    }

    public Vector2 GetMoveDirection()
    {
        return moveDirection;
    }

    public float GetSpeed()
    {
        return speed;
    }

    public float GetTargetSpeed()
    {
        return targetSpeed;
    }

    public float GetTileDiameter()
    {
        return tileDiameter;
    }

    public float GetStartingTileDiameter()
    {
        return startingTileDiameter;
    }

    public float GetPctFed()
    {
        return pctFedUntilLengthen;
    }

    public int GetTailLength()
    {
        return tailLength;
    }

    void Lengthen()
    {
        bodyparts.Add(Instantiate(prefabBody, transform.parent).GetComponent<SnakeBody>());
        if (bodyparts.Count % 10 == 0 && eatSelf == null)
        {
            StartCoroutine(IncreaseSpeed(speed, 10f, 10f, false));
        }
        SetTail();
        tail.name = "Body (" + (bodyparts.Count - 1) + ")";
    }

    void Grow(float _scaleFactor)
    {
        CapsuleCollider2D capsuleCollider = GetComponent<CapsuleCollider2D>();
        capsuleCollider.size *= _scaleFactor;
        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.radius *= _scaleFactor;
        tileDiameter *= _scaleFactor;
        rect.sizeDelta *= _scaleFactor;
        GameObject headGraphic = transform.GetChild(1).gameObject;
        RectTransform gRect = headGraphic.GetComponent<RectTransform>();
        gRect.sizeDelta *= _scaleFactor;
        
        GameObject leftEye = headGraphic.transform.GetChild(0).gameObject;
        GameObject rightEye = headGraphic.transform.GetChild(1).gameObject;
        RectTransform leRect = leftEye.GetComponent<RectTransform>();
        RectTransform reRect = rightEye.GetComponent<RectTransform>();
        leRect.offsetMax *= _scaleFactor;
        leRect.offsetMin *= _scaleFactor;
        reRect.offsetMax *= _scaleFactor;
        reRect.offsetMin *= _scaleFactor;
    }

    void SetTail()
    {
        tail = bodyparts[bodyparts.Count - 1];
    }

    void TailIsEdibleCheck()
    {
        if (bodyparts.Count >= minLengthToEatSelf)
        {
            onSetTail();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        switch ((GameManager.layers)collision.gameObject.layer)
        {
            case GameManager.layers.obstacle:
                if (isInvincible)
                {
                    return;
                }

                if (PlayFabController.PFC.IsTempLogin() == false || gc.GetCurScore() < 10)
                {
                    SetMessageToPressSpace();
                }

                onDie();
                break;
            case GameManager.layers.pickup:
                if (moveMode != GameManager.moveMode.grid && collision.gameObject.GetComponent<SnakeBody>() == null)
                {
                    return;
                }

                Fish fish = collision.gameObject.GetComponent<Fish>();
                
                if (Fish.GetStandardFishHeight() * fish.GetScale() > tileDiameter && fish.GetHP() > 0f)
                {
                    onDie();
                    OverlayCanvas.oc.textMessage.text = "Fish too big to eat!";
                    Invoke("SetMessageToPressSpace", 3f);
                }
                break;
            case GameManager.layers.edibleTail:
                break;
            default:
                break;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        switch ((GameManager.layers)collision.gameObject.layer)
        {
            case GameManager.layers.obstacle:
                break;
            case GameManager.layers.pickup:

                if (moveMode != GameManager.moveMode.grid && collision.gameObject.GetComponent<SnakeBody>() == null)
                {
                    return;
                }

                Fish fish = collision.gameObject.GetComponent<Fish>();

                if (Fish.GetStandardFishHeight() * fish.GetScale() > tileDiameter && fish.GetHP() > 0f)
                {
                    return;
                }

                pctFedUntilLengthen += fish.GetFood() * startingTileDiameter / tileDiameter;

                onEat(fish);
                onDoneEating(fish);

                while (pctFedUntilLengthen > 1f)
                {
                    pctFedUntilLengthen--;
                    onLengthen();
                }
                break;
            case GameManager.layers.edibleTail:
                if (moveMode == GameManager.moveMode.grid)
                {
                    eatSelf = StartCoroutine(EatSelf());
                }
                break;
            default:
                break;
        }
    }

    void SetMessageToPressSpace()
    {
        OverlayCanvas.oc.textMessage.text = "Press Space to play again.";
    }

    IEnumerator EatSelf()
    {
        float waitTime = 1f;
        isInvincible = true;

        //record info
        int numBodyParts = bodyparts.Count;
        int cutOffIndex = numBodyParts / 2;
        SnakeBody cutoffBodyPart = bodyparts[cutOffIndex];

        //stop movement and wait for a bit
        StopMovement();
        yield return new WaitForSeconds(waitTime);

        //change body parts that are going to be eaten to pickup layer
        int numFuncs = onDoneEating.GetInvocationList().Length;
        for (int i = cutOffIndex; i < numBodyParts; i++)
        {
            bodyparts[i].gameObject.AddComponent<Fish>();
            bodyparts[i].gameObject.layer = (int)GameManager.layers.pickup;
        }
        yield return new WaitUntil(() => onDoneEating.GetInvocationList().Length == numFuncs + numBodyParts - cutOffIndex);

        //destroy all 3 edible tail parts
        for (int i = 0; i < tailLength; i++)
        {
            bodyparts.Remove(tail);
            onEat(tail.GetComponent<Fish>());
            onDoneEating(tail.GetComponent<Fish>());
            yield return new WaitUntil(() => tail == null);
            SetTail();
        }

        //move player along body and destroy each part as it goes until you get to cut-off point
        SetMoveMode(GameManager.moveMode.free);
        while (cutoffBodyPart != null)
        {
            yield return new WaitUntil(() => tail == null);
            bodyparts.RemoveAt(bodyparts.Count - 1);
            SetTail();
        }

        //stop movement and wait for a bit
        StopMovement();
        speed = 0f;
        yield return new WaitForSeconds(waitTime);
        
        //grow thicker
        onGrow(1.1f);
        pctFedUntilLengthen = 1f;
        minLengthToEatSelf += 2; //require longer tail to ouroboros next time

        //give control back to player
        desiredDirection = (transform.position - bodyparts[0].transform.position).normalized;
        moveDirection = desiredDirection;
        gm.RotateRect(rect, moveDirection);
        SetMoveMode(GameManager.moveMode.transition);

        //speed back up from 0 speed
        StartCoroutine(IncreaseSpeed(0f, targetSpeed, 1.5f, true));
        yield return new WaitUntil(() => speed == targetSpeed);

        //cleanup
        isInvincible = moveMode != GameManager.moveMode.grid;
        eatSelf = null;
    }

    IEnumerator IncreaseSpeed(float _startingSpeed, float _speedIncrease, float _timeUntilFinish, bool _overrideWait)
    {
        float timer = 0f;
        targetSpeed = Mathf.Min(_startingSpeed + _speedIncrease, maxSpeed);

        while (timer < _timeUntilFinish)
        {
            yield return new WaitUntil(() => (eatSelf == null || _overrideWait));
            float pctTimer = timer / _timeUntilFinish;
            float newSpeed = _startingSpeed * (1 - pctTimer) + targetSpeed * pctTimer;
            speed = newSpeed;

            yield return null;
            timer += Time.deltaTime;
        }

        speed = targetSpeed;
    }

    void StopMovement()
    {
        SetMoveMode(GameManager.moveMode.stop);
    }

    void SetMoveMode(GameManager.moveMode _mode)
    {
        moveMode = _mode;
        onSetMoveMode(_mode);
    }

    IEnumerator WaitForFirstKeyPress()
    {
        yield return new WaitUntil(() => (Input.GetAxis("Horizontal") > 0f || Input.GetAxis("Vertical") != 0));
        
        if (Input.GetAxis("Horizontal") > 0f)
        {
            desiredDirection = Vector2.right;
        }
        else if (Input.GetAxis("Vertical") != 0)
        {
            desiredDirection = Vector2.up * Mathf.Sign(Input.GetAxis("Vertical"));
            onTurn(new Vector4(transform.position.x, transform.position.y, desiredDirection.x, desiredDirection.y));
            gm.RotateRect(rect, desiredDirection);
        }

        moveDirection = desiredDirection;
        onFirstKeyPress(GameManager.moveMode.grid);

        OverlayCanvas.oc.textMessage.text = "";
    }

    void OnDisable()
    {
        UnregisterOnLengthen(Lengthen);
        UnregisterOnLengthen(TailIsEdibleCheck);
        UnregisterOnDie(StopMovement);
        UnregisterOnGrow(Grow);
        UnregisterOnFirstKeyPress(SetMoveMode);
    }

    public static void RegisterOnLengthen(GameManager.VoidNull _funcToAdd)
    {
        onLengthen += _funcToAdd;
    }

    public static void UnregisterOnLengthen(GameManager.VoidNull _funcToRemove)
    {
        onLengthen -= _funcToRemove;
    }

    public static void RegisterOnSetTail(GameManager.VoidNull _funcToAdd)
    {
        onSetTail += _funcToAdd;
    }

    public static void UnregisterOnSetTail(GameManager.VoidNull _funcToRemove)
    {
        onSetTail -= _funcToRemove;
    }
    public static void RegisterOnDie(GameManager.VoidNull _funcToAdd)
    {
        onDie += _funcToAdd;
    }

    public static void UnregisterOnDie(GameManager.VoidNull _funcToRemove)
    {
        onDie -= _funcToRemove;
    }

    public static void RegisterOnEat(GameManager.VoidFish _funcToAdd)
    {
        onEat += _funcToAdd;
    }

    public static void UnregisterOnEat(GameManager.VoidFish _funcToRemove)
    {
        onEat -= _funcToRemove;
    }

    public static void RegisterOnDoneEating(GameManager.VoidFish _funcToAdd)
    {
        onDoneEating += _funcToAdd;
    }

    public static void UnregisterOnDoneEating(GameManager.VoidFish _funcToRemove)
    {
        onDoneEating -= _funcToRemove;
    }

    public static void RegisterOnGrow(GameManager.VoidFloat _funcToAdd)
    {
        onGrow += _funcToAdd;
    }

    public static void UnregisterOnGrow(GameManager.VoidFloat _funcToRemove)
    {
        onGrow -= _funcToRemove;
    }
    public static void RegisterOnTurn(GameManager.VoidVector4 _funcToAdd)
    {
        onTurn += _funcToAdd;
    }

    public static void UnregisterOnTurn(GameManager.VoidVector4 _funcToRemove)
    {
        onTurn -= _funcToRemove;
    }
    public static void RegisterOnSetMoveMode(GameManager.VoidMoveMode _funcToAdd)
    {
        onSetMoveMode += _funcToAdd;
    }

    public static void UnregisterOnSetMoveMode(GameManager.VoidMoveMode _funcToRemove)
    {
        onSetMoveMode -= _funcToRemove;
    }
    public static void RegisterOnFirstKeyPress(GameManager.VoidMoveMode _funcToAdd)
    {
        onFirstKeyPress += _funcToAdd;
    }

    public static void UnregisterOnFirstKeyPress(GameManager.VoidMoveMode _funcToRemove)
    {
        onFirstKeyPress -= _funcToRemove;
    }

    void Movement(Vector2 _direction)
    {
        transform.Translate(_direction * speed * Time.deltaTime, Space.World);
    }

    bool CheckKeyPress()
    {
        bool wasKeyPress = false;
        foreach (KeyCode key in controls.Keys)
        {
            if (Input.GetKeyDown(key) && Vector2.Dot(moveDirection, controls[key]) >= -.5f)
            {
                desiredDirection = controls[key];
                wasKeyPress = true;
                break;
            }
        }
        return wasKeyPress;
    }

    void Update()
    {
        switch (moveMode)
        {
            case GameManager.moveMode.grid:
                Movement(moveDirection);
                CheckKeyPress();
                ChangeDirection(desiredDirection);
                break;
            case GameManager.moveMode.transition:
                Movement(moveDirection);
                if (CheckKeyPress())
                {
                    moveMode = GameManager.moveMode.grid;
                    isInvincible = eatSelf != null;
                }
                break;
            case GameManager.moveMode.free:
                if (tail == null)
                {
                    break;
                }
                Vector2 moveDir = (tail.transform.position - transform.position).normalized;
                gm.RotateRect(rect, moveDir);
                Movement(moveDir);
                break;
            default:
                break;
        }
    }
}
