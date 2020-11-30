using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using CodeMonkey.Utils;
using CodeMonkey;
using System;

public class Level : MonoBehaviour
{
    private const float CAMERA_ORTHO_SIZE = 50f;
    private const float PIPE_WIDTH = 7.8f;
    private const float PIPE_HEAD_HEIGHT = 3.75f;
    private const float PIPE_MOVE_SPEED = 30f;
    private const float PIPE_DESTROY_X_POSITION = -100f;
    private const float PIPE_SPAWN_X_POSITION = 100f;
    private const float BIRD_X_POSITION = 0f;
    private const float GROUND_DESTROY_X_POSITION = -200f;
    private const float CLOUD_DESTROY_X_POSITION = -160f;
    private const float CLOUD_SPAWN_X_POSITION = 160f;

    private float pipeSpawnTimer;
    private float pipeSpawnTimerMax;
    private float gapSize;
    private List<Pipe> pipeList;
    private int pipeSpawned = 0;
    private int pipesPassedCount = 0;
    private State state;
    private List<Transform> groundList;
    private List<Transform> cloudList;
    private float cloudSpawnTimer;

    public static Level GetInstance()
    {
        return Instance;
    }

    private static Level Instance;
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard,
        Impossible
    }

    private enum State
    {
        WaitingToStart,
        Playing,
        BirdDead,
    }


    private void Awake()
    {
        SpawnInitialGround();
        SpawnInitialClouds();
        Instance = this;
        pipeList = new List<Pipe>();
        pipeSpawnTimerMax = 1f;
        gapSize = 50f;
        SetDifficulty(Difficulty.Easy);
        state = State.WaitingToStart;
    }

    private void Start()
    {
        Bird.GetInstance().OnDied += Bird_OnDied;
        Bird.GetInstance().OnStartedPlaying += Bird_OnStartedPlaying;
    }

    private void Bird_OnDied(object sender, System.EventArgs e)
    {
        state = State.BirdDead;
    }

    private void Bird_OnStartedPlaying(object sender, System.EventArgs e)
    {
        state = State.Playing;
    }

    private void Update()
    {
        if (state == State.Playing)
        {
            HandlePipeMovement();
            HandlePipeSpawning();
            HandleGroundMovement();
            HandleClouds();
        }
    }

    private Transform GetCloudPrefabTransform()
    {
        switch(UnityEngine.Random.Range(0, 3))
        {
            default:
            case 0:
                return GameAssets.GetInstance().pfCloud_1;
                break;
            case 1:
                return GameAssets.GetInstance().pfCloud_2;
                break;
            case 2:
                return GameAssets.GetInstance().pfCloud_3;
                break;
        }
    }
    private void HandleClouds()
    {
        //Handle cloud spawning
        cloudSpawnTimer -= Time.deltaTime;
        if (cloudSpawnTimer < 0)
        {
            float cloudSpawnTimerMax = 7f;
            float cloudY = 30f;
            cloudSpawnTimer = cloudSpawnTimerMax;
            Transform cloudTransform = Instantiate(GetCloudPrefabTransform(), new Vector3(CLOUD_SPAWN_X_POSITION, cloudY, 0), Quaternion.identity);
            cloudList.Add(cloudTransform);
        }

        //Handle cloud movement
        for (int i = 0; i < cloudList.Count; i++)
        {
            Transform cloudTransform = cloudList[i];
            //Move cloud with less speed than pipes for Parallax effect
            cloudTransform.position += new Vector3(-1, 0, 0) * PIPE_MOVE_SPEED * Time.deltaTime * .7f;

            if (cloudTransform.position.x < CLOUD_DESTROY_X_POSITION)
            {
                //Cloud destruction
                Destroy(cloudTransform.gameObject);
                cloudList.RemoveAt(i);
                i--;
            }
        }
    }
        private void SpawnInitialClouds()
        {
            cloudList = new List<Transform>();
            Transform cloudTransform;
            float cloudY = 30f;
            cloudTransform = Instantiate(GetCloudPrefabTransform(), new Vector3(0, cloudY, 0), Quaternion.identity);
            cloudList.Add(cloudTransform);

        }

        private void SpawnInitialGround()
        {
            groundList = new List<Transform>();
            Transform groundTransform;
            float groundY = -46.7f;
            float groundWidth = 222f;
            groundTransform = Instantiate(GameAssets.GetInstance().pfGround, new Vector3(0, groundY, 0), Quaternion.identity);
            groundList.Add(groundTransform);
            groundTransform = Instantiate(GameAssets.GetInstance().pfGround, new Vector3(groundWidth, groundY, 0), Quaternion.identity);
            groundList.Add(groundTransform);
            groundTransform = Instantiate(GameAssets.GetInstance().pfGround, new Vector3(groundWidth * 2f, groundY, 0), Quaternion.identity);
            groundList.Add(groundTransform);
        }

        private void HandleGroundMovement()
        {
            foreach (Transform groundTransform in groundList)
            {
                groundTransform.position += new Vector3(-1, 0, 0) * PIPE_MOVE_SPEED * Time.deltaTime;

                if (groundTransform.position.x < GROUND_DESTROY_X_POSITION)
                {
                    //Ground passed the left side, relocate on the right side
                    //Find right most x Position
                    float rightMostXPosition = -100f;
                    for (int i = 0; i < groundList.Count; i++)
                    {
                        if (groundList[i].position.x > rightMostXPosition)
                        {
                            rightMostXPosition = groundList[i].position.x;
                        }
                    }
                    //Place ground on the right most position
                    float groundWidth = 221.5f;
                    groundTransform.position = new Vector3(rightMostXPosition + groundWidth, groundTransform.position.y, groundTransform.position.z);

                }
            }
        }

        private void HandlePipeSpawning()
        {
            pipeSpawnTimer -= Time.deltaTime;
            if (pipeSpawnTimer < 0)
            {
                //Time to spawn new pipe
                float heightEdgeLimit = 10f;
                float minheight = gapSize * .5f + heightEdgeLimit;
                float totalHeight = CAMERA_ORTHO_SIZE * 2f;
                float maxHeight = totalHeight - gapSize * .5f - heightEdgeLimit;

                float height = UnityEngine.Random.Range(minheight, maxHeight);


                pipeSpawnTimer += pipeSpawnTimerMax;
                CreateGapPipes(height, gapSize, PIPE_SPAWN_X_POSITION);
            }
        }

        private void HandlePipeMovement()
        {
            for (int i = 0; i < pipeList.Count; i++)
            {
                Pipe pipe = pipeList[i];
                bool isToTheRightOfBird = pipe.GetXPosition() > BIRD_X_POSITION;
                pipe.Move();
                if (pipe.isBottom && isToTheRightOfBird && pipe.GetXPosition() <= BIRD_X_POSITION)
                {
                    // Pipe passed bird
                    pipesPassedCount++;
                    SoundManager.PlaySound(SoundManager.Sound.Score);
                }

                if (pipe.GetXPosition() < PIPE_DESTROY_X_POSITION)
                {
                    //Destroy pipe
                    pipe.DestroySelf();
                    pipeList.Remove(pipe);
                    i--;
                }
            }
        }


        private void SetDifficulty(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    gapSize = 50f;
                    pipeSpawnTimerMax = 1.2f;
                    break;
                case Difficulty.Medium:
                    gapSize = 40f;
                    pipeSpawnTimerMax = 1.1f;
                    break;
                case Difficulty.Hard:
                    gapSize = 35f;
                    pipeSpawnTimerMax = 1.0f;
                    break;
                case Difficulty.Impossible:
                    gapSize = 24f;
                    pipeSpawnTimerMax = .8f;
                    break;
            }

        }
        private Difficulty GetDifficulty()
        {
            if (pipeSpawned >= 50) return Difficulty.Impossible;
            if (pipeSpawned >= 30) return Difficulty.Hard;
            if (pipeSpawned >= 20) return Difficulty.Medium;
            return Difficulty.Easy;
        }

        private void CreateGapPipes(float gapY, float gapSize, float xPosition)
        {
            CreatePipe(gapY - gapSize * .5f, xPosition, true);
            CreatePipe(CAMERA_ORTHO_SIZE * 2f - gapY - gapSize * .5f, xPosition, false);
            pipeSpawned++;
            SetDifficulty(GetDifficulty());
        }
        private void CreatePipe(float height, float xPosition, bool createBottom)
        {
            //Pipe head
            Transform pipeHead = Instantiate(GameAssets.GetInstance().pfPipeHead);
            float pipeHeadYPosition;
            if (createBottom)
            {
                pipeHeadYPosition = -CAMERA_ORTHO_SIZE + height - PIPE_HEAD_HEIGHT * .5f;
            }
            else
            {
                pipeHeadYPosition = CAMERA_ORTHO_SIZE - height + PIPE_HEAD_HEIGHT * .5f;
            }
            pipeHead.position = new Vector3(xPosition, pipeHeadYPosition);

            //Pipe body
            Transform pipeBody = Instantiate(GameAssets.GetInstance().pfPipeBody);
            float pipeBodyYPosition;
            if (createBottom)
            {
                pipeBodyYPosition = -CAMERA_ORTHO_SIZE;

            }
            else
            {
                pipeBodyYPosition = CAMERA_ORTHO_SIZE;
                pipeBody.localScale = new Vector3(1, -1, 1);
            }
            pipeBody.position = new Vector3(xPosition, pipeBodyYPosition);

            SpriteRenderer pipeBodySpriteRenderer = pipeBody.GetComponent<SpriteRenderer>();
            pipeBodySpriteRenderer.size = new Vector2(PIPE_WIDTH, height);

            BoxCollider2D pipeBodyBoxCollider = pipeBody.GetComponent<BoxCollider2D>();
            pipeBodyBoxCollider.size = new Vector2(PIPE_WIDTH, height);
            pipeBodyBoxCollider.offset = new Vector2(0f, height * .5f);

            Pipe pipe = new Pipe(pipeHead, pipeBody, createBottom);
            pipeList.Add(pipe);

        }

private class Pipe
    {
        private Transform pipeHeadTransform;
        private Transform pipeBodyTransform;
        public bool isBottom { get; }

        public Pipe(Transform pipeHeadTransform, Transform pipeBodyTransform, bool createBottom)
        {
            this.pipeHeadTransform = pipeHeadTransform;
            this.pipeBodyTransform = pipeBodyTransform;
            this.isBottom = createBottom;
        }

        public void Move()
        {
            pipeBodyTransform.position += new Vector3(-1, 0, 0) * PIPE_MOVE_SPEED * Time.deltaTime;
            pipeHeadTransform.position += new Vector3(-1, 0, 0) * PIPE_MOVE_SPEED * Time.deltaTime;
        }

        public float GetXPosition()
        {
            return pipeHeadTransform.position.x;
        }

        public void DestroySelf()
        {
            Destroy(pipeHeadTransform.gameObject);
            Destroy(pipeBodyTransform.gameObject);
        }
    }

    public int GetPipesPassedCount()
    {
        return pipesPassedCount;
    }
}
