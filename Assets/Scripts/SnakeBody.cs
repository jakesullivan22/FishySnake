using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SnakeBody : MonoBehaviour
{
    static PlayerController pc = PlayerController.pc;
    static GameManager gm = GameManager.gm;
    static int numStartingBodies = 3;

    [SerializeField] Vector2 moveDirection;
    Queue<Vector4> turnQueue = new Queue<Vector4>(); //point and then direction
    GameManager.moveMode moveMode;

    Image myImage;
    RectTransform myRect;
    CircleCollider2D myCollider;

    Transform priorBody;
    int bodyIndex; //index in the PlayerController.bodyparts array 
    float distanceToPrior;

    void Awake()
    {
        myImage = GetComponent<Image>();
        myCollider = GetComponent<CircleCollider2D>();
        myRect = GetComponent<RectTransform>();
    }

    void Start()
    {
        gm = GameManager.gm;
        pc = PlayerController.pc;
        PlayerController.RegisterOnTurn(QueueTurn);
        PlayerController.RegisterOnSetMoveMode(SetMoveMode);
        PlayerController.RegisterOnSetTail(SetTailColorAndLayer);
        PlayerController.RegisterOnGrow(Resize);
        PlayerController.RegisterOnLengthen(UnregisterResizeTail);

        for (int i = 0; i < transform.parent.childCount; i++)
        {
            if (transform.parent.GetChild(i) == transform)
            {
                bodyIndex = i - 1;
                break;
            }
        }
        priorBody = transform.parent.GetChild(bodyIndex);

        SnakeBody priorSnakeBody = priorBody.GetComponent<SnakeBody>();

        if (priorSnakeBody != null)
        {
            moveMode = priorSnakeBody.moveMode;
            turnQueue = CopyQueue(priorSnakeBody); 
            moveDirection = priorSnakeBody.moveDirection;
            Resize(pc.GetTileDiameter() / pc.GetStartingTileDiameter() * Mathf.Min(1f, pc.GetPctFed()));
            if (moveMode == GameManager.moveMode.grid)
            {
                transform.position = (Vector2)priorBody.position - moveDirection * myRect.rect.height;
            }
            else if (moveMode != GameManager.moveMode.stop)
            {
                transform.position = priorBody.position + (priorBody.position - priorSnakeBody.priorBody.transform.position).normalized * distanceToPrior;
            }

            if (priorSnakeBody.gameObject.layer == (int)GameManager.layers.edibleTail)
            {
                SetTailColorAndLayer();
            }
        }
        else
        {
            Resize(1f);
        }

        if (bodyIndex < numStartingBodies)
        {
            SetMoveMode(GameManager.moveMode.stop);
        }
        else
        {
            PlayerController.RegisterOnEat(ResizeTail);
        }
    }

    void SetMoveMode(GameManager.moveMode _moveMode)
    {
        moveMode = _moveMode;
    }

    Queue<Vector4> CopyQueue(SnakeBody _snakeBody)
    {
        Queue<Vector4> queue = new Queue<Vector4>();

        if (_snakeBody == null || _snakeBody.turnQueue == null)
        {
            return queue;
        }

        Vector4[] array = _snakeBody.turnQueue.ToArray();
        for (int i = 0; i < array.Length; i++)
        {
            queue.Enqueue(array[i]);
        }
        return queue;
    }

    void QueueTurn(Vector4 _pointDirection)
    {
        turnQueue.Enqueue(_pointDirection);
    }

    void ChangeDirection()
    {
        Vector4 pointDirection = turnQueue.Peek();
        Vector2 turnPosition = new Vector2(pointDirection.x, pointDirection.y);
        float offset = Vector2.Dot((Vector2)transform.position - turnPosition, moveDirection);

        if (offset > 0)
        {
            Vector2 direction = new Vector2(pointDirection.z, pointDirection.w);
            transform.position = turnPosition + direction * offset;
            moveDirection = direction;
            turnQueue.Dequeue();
            gm.RotateRect(myRect, moveDirection);
        }
    }

    void SetTailColorAndLayer()
    {
        int numBodyParts = transform.parent.childCount - 1;
        if (bodyIndex == 0 || bodyIndex == 1)
        {
            return;
        }
        else if (bodyIndex > numBodyParts - (pc.GetTailLength() + 1))
        {
            gameObject.layer = (int)GameManager.layers.edibleTail;
            myImage.color = Color.Lerp(Color.Lerp(Color.red, Color.white, .5f), Color.yellow, .15f);
        }
        else
        {
            gameObject.layer = (int)GameManager.layers.obstacle;
            myImage.color = Color.green;
        }
    }

    void Resize(float _scaleFactor)
    {
        if (myRect == null)
        {
            myRect = GetComponent<RectTransform>();
        }

        float tileDiameter = pc.GetTileDiameter();
        float priorHeight = myRect.rect.height;
        if (myRect.rect.height * _scaleFactor > tileDiameter)
        {
            _scaleFactor = tileDiameter / myRect.rect.height;
        }

        myCollider.radius *= _scaleFactor;
        myRect.sizeDelta *= _scaleFactor;
        distanceToPrior = tileDiameter * .5f + myRect.rect.height * .5f;

        if (moveMode == GameManager.moveMode.grid)
        {
            transform.position = (Vector2)transform.position - moveDirection * (myRect.rect.height - priorHeight);
        }
    }

    void ResizeTail(Fish _fish)
    {
        float currentPct = myRect.rect.height / priorBody.GetComponent<RectTransform>().rect.height;
        if (currentPct == 1f)
        {
            return;
        }
        Resize(Mathf.Min(1f, pc.GetPctFed()) / currentPct);
    }

    void UnregisterResizeTail()
    {
        PlayerController.UnregisterOnEat(ResizeTail);
        PlayerController.UnregisterOnLengthen(UnregisterResizeTail);
    }

    void OnDisable()
    {
        PlayerController.UnregisterOnTurn(QueueTurn);
        PlayerController.UnregisterOnSetMoveMode(SetMoveMode);
        PlayerController.UnregisterOnSetTail(SetTailColorAndLayer);
        PlayerController.UnregisterOnGrow(Resize);
        PlayerController.UnregisterOnEat(ResizeTail);
    }

    void Update()
    {
        switch (moveMode)
        {
            case GameManager.moveMode.grid:
                transform.Translate(moveDirection * pc.GetSpeed() * Time.deltaTime, Space.World);

                if (turnQueue.Count > 0)
                {
                    ChangeDirection();
                }
                break;

            case GameManager.moveMode.free: //movement after or during ouroboros
            case GameManager.moveMode.transition:
                if (priorBody == null)
                {
                    break;
                }

                Vector2 moveDir = priorBody.transform.position - transform.position;
                if (Mathf.Pow(distanceToPrior, 2) > Vector2.SqrMagnitude(moveDir)) //too close to prior body part so don't move
                {
                    break;
                }

                moveDir = moveDir.normalized;
                transform.Translate(moveDir * pc.GetTargetSpeed() * Time.deltaTime, Space.World);

                if (moveMode == GameManager.moveMode.free)
                {
                    break;
                }

                //at this point, we are transitioning to free movement

                //when moving in a grid direction and prior bodypart is in grid moveMode, switch back to grid moveMode
                SnakeBody priorSnakeBody = priorBody.GetComponent<SnakeBody>();

                float requiredAngleMatchToSnapToGrid = .99f;
                bool hasPriorSnakeBody = priorSnakeBody != null;

                bool priorIsOnGrid = false;
                bool angleIsGood = false;
                bool headIsOnGrid = false;
                bool headIsFullSpeed = false;
                bool headAngleIsGood = false;

                if (hasPriorSnakeBody)
                {
                    priorIsOnGrid = priorSnakeBody.moveMode == GameManager.moveMode.grid;
                    angleIsGood = Vector2.Dot(priorSnakeBody.moveDirection, moveDir) > requiredAngleMatchToSnapToGrid;
                }
                else
                {
                    headIsOnGrid = pc.GetMoveMode() == GameManager.moveMode.grid;
                    headIsFullSpeed = pc.GetSpeed() == pc.GetTargetSpeed();
                    headAngleIsGood = Vector2.Dot(pc.GetMoveDirection(), moveDir) > requiredAngleMatchToSnapToGrid;
                }
                
                if ((hasPriorSnakeBody && priorIsOnGrid && angleIsGood) || (hasPriorSnakeBody == false && headIsOnGrid && headIsFullSpeed && headAngleIsGood))
                {
                    moveDirection = new Vector2(Mathf.RoundToInt(moveDir.x), Mathf.RoundToInt(moveDir.y));
                    transform.position = (Vector2)priorBody.position - moveDirection * myRect.rect.height; ;
                    turnQueue = CopyQueue(priorSnakeBody);
                    moveMode = GameManager.moveMode.grid;
                }
                break;

            default:
                break;
        }
    }
}
