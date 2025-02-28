using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lofelt.NiceVibrations;
using UnityEngine.UI;

public enum PlayerType
{
    human,
    bot
}
public class PlayerCollisions : MonoBehaviour
{
    public PlayerType playerType;
    public Transform stackPos;
    public GameObject crown;
    public int curStackCount;
    public int newStackCount;

    public bool grounded;
    public bool jumping;
    public bool bouncing;
    public bool water;
    public bool endPodReached;
    public bool canPlaceLog;
    public bool climbing;
    public float logSpawnDelay;
    public List<GameObject> logs;
    public bool botDeath;
    public int botID;
    public GameObject windFx;
    public GameObject lastPodOn;
    public int bonusCoinX;
    public int randStackColor;

    public float distFromEnd;
    public int rank;
    public bool cannotBuild;
    Camera cam;
    public int brickPlaced;
    public GameObject boostFX;
    public bool finishCrossed;
    public bool playerEndStopped;

    private void Awake()
    {
        
    }
    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        crown.SetActive(false);
        randStackColor = Random.Range(0, GameManager.instance.podMats.Length);
        
    }
    private void Update()
    {
        if (jumping)
        {
            transform.position += transform.forward * Time.deltaTime * 3f;
        }
        if (bouncing)
        {
            transform.position += transform.forward * Time.deltaTime * 7f;
            transform.position += Vector3.up * Time.deltaTime * 12f;
        }
        if (canPlaceLog && !GameManager.instance.dead && !bouncing)
        {
            if(logSpawnDelay >= 0.125f)
                PlaceLog();
            else
                logSpawnDelay += Time.deltaTime;
        }
        if (grounded)
            transform.GetComponent<PlayerMovementTwo>().speed = 7;
        if(GameManager.instance.gameStart && !finishCrossed)
            GetDistanceFromFinish();

        if (GameManager.instance.gameStart && playerType == PlayerType.human)
            CamPosAdjust();

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("logpickup"))
        {
            Destroy(other.gameObject);
            curStackCount++;
            transform.gameObject.GetComponent<PlayerMovementTwo>().anim.SetBool("carry", true);

            GameObject go = Instantiate(GameManager.instance.logStackObj, new Vector3(stackPos.transform.position.x, stackPos.transform.position.y + 0.15f * curStackCount, stackPos.transform.position.z), Quaternion.identity);
            go.transform.parent = stackPos.transform.GetChild(0);
            go.transform.DOLocalRotateQuaternion(Quaternion.Euler(0, 0, 0), 0.2f);
            go.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.2f);
            logs.Add(go);

            GameObject fx = Instantiate(GameManager.instance.stackFX, new Vector3(stackPos.transform.position.x, stackPos.transform.position.y + 0.15f * curStackCount, stackPos.transform.position.z), Quaternion.identity);
            Destroy(fx, 1f);

            if (playerType == PlayerType.human)
            {
                HapticPatterns.PlayConstant(0.2f, 0f, 0.1f);
                SoundManager.Instance.PlaySFX(SoundManager.Instance.logPickSFX);
                UIManager.instance.txtLogCount.transform.gameObject.SetActive(true);
                newStackCount++;
                UIManager.instance.txtLogCount.text = "+" + newStackCount.ToString();
                UIManager.instance.txtLogCount.transform.DOScale(new Vector3(-1.5f, 1.5f, 1.5f), 0.1f).OnComplete(() =>
                {
                    UIManager.instance.txtLogCount.transform.DOScale(new Vector3(-1f, 1f, 1f), 0.05f);
                });

                UIManager.instance.txtLogCount.transform.DOMoveY(UIManager.instance.txtLogCount.transform.position.y, 0.1f).SetDelay(2).OnComplete(() =>
                {
                    newStackCount = 0;
                    UIManager.instance.txtLogCount.transform.gameObject.SetActive(false);
                });
                
                //UIManager.instance.txtLogCount.transform.DOMoveY(UIManager.instance.txtLogCount.transform.position.y + 0.015f * curStackCount, 0.1f).SetDelay(2).OnComplete(()=>
                //{
                //    UIManager.instance.txtLogCount.DOFade(1f, 0.1f);
                    
                //});
            }
            if (playerType == PlayerType.bot)
            {
                go.transform.GetComponent<MeshRenderer>().material = GameManager.instance.podMats[randStackColor];
            }
        }
        if (other.gameObject.CompareTag("water") && !GameManager.instance.dead && !climbing)
        {
            bouncing = false;
            water = true;

            if (curStackCount > 0)
            {
                canPlaceLog = true;
                logSpawnDelay = 0.125f;
            }            
            else if (GameManager.instance.finishCrossed && playerType == PlayerType.human)
            {
                //move to last end pod player was on, back to finishline if none 
                if(lastPodOn != null)
                {                  
                    transform.DOMove(new Vector3(lastPodOn.transform.position.x, lastPodOn.transform.position.y + 1.2f, lastPodOn.transform.position.z), 0.5f).OnComplete(()=>
                    {
                        Camera.main.transform.parent = null;
                        bonusCoinX = lastPodOn.GetComponent<EndPod>().endPodID;
                        StopPlayerAtEnd();
                        lastPodOn.transform.GetChild(1).gameObject.SetActive(true);
                        lastPodOn.transform.GetChild(1).GetComponent<ParticleSystem>().loop = true;
                        lastPodOn.transform.GetChild(1).GetComponent<ParticleSystem>().startColor = lastPodOn.GetComponent<MeshRenderer>().material.color;
                    });
                    
                }
                else
                {
                    transform.DOMove(new Vector3(GameManager.instance.finishLine.transform.position.x, GameManager.instance.finishLine.transform.position.y + 1f, GameManager.instance.finishLine.transform.position.z), 0.5f).OnComplete(() =>
                    {
                        bonusCoinX = 1;
                        StopPlayerAtEnd();
                    }); ;
                    
                }
            }
            else//DEATH
            {
                if (playerType == PlayerType.human)
                {
                    HapticPatterns.PlayConstant(0.5f, 0f, 0.2f);
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.splashSFX);
                    Camera.main.transform.parent = null;
                    GameManager.instance.dead = true;
                    UIManager.instance.panelGame.SetActive(false);
                    StartCoroutine(DeathRoutine());             
                }
                if (playerType == PlayerType.bot)
                {
                    botDeath = true;
                    Destroy(this.gameObject);
                }
                GameObject fx = Instantiate(GameManager.instance.splashFX, new Vector3(transform.position.x, transform.position.y, transform.position.z), GameManager.instance.splashFX.transform.rotation);
                Destroy(fx, 1f);

            }

        }
        if (other.gameObject.CompareTag("bounce") && !GameManager.instance.dead)
        {
            //transform.DOMoveY(transform.position.y + 30f, 2f).SetLoops(2, LoopType.Yoyo).OnComplete(()=>
            //{
            //    bouncing = false;
            //});
            grounded = false;
            bouncing = true;
            jumping = false;
            windFx.SetActive(false);
           
            if (playerType == PlayerType.human)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.bounceSFX);
                HapticPatterns.PlayConstant(0.2f, 0f, 0.15f);
                boostFX.SetActive(false);
            }              
        }
        if (other.gameObject.CompareTag("finish") && !GameManager.instance.dead)
        {
            if (playerType == PlayerType.human)
              BotManager.instance.playerFinalPos = BotManager.instance.playerPos;
            if (playerType == PlayerType.bot)
            {
                finishCrossed = true;
                transform.gameObject.GetComponent<BoxCollider>().enabled = false;
                if(rank == 1)
                {
                    ThrowStack();
                    transform.DOMove(new Vector3(GameManager.instance.finishLinePositions[rank - 1].transform.position.x, GameManager.instance.finishLinePositions[rank - 1].transform.position.y + 1f, GameManager.instance.finishLinePositions[rank - 1].transform.position.z), 0.5f).OnComplete(() =>
                    {
                        GetComponent<PlayerMovementTwo>().anim.SetBool("dance", true);
                    }); ;
                    
                    //throw + dance
                    
                }
                if(rank > 1)
                {
                    ThrowStack();
                    transform.DOMove(new Vector3(GameManager.instance.finishLinePositions[rank - 1].transform.position.x, GameManager.instance.finishLinePositions[rank - 1].transform.position.y + 1f, GameManager.instance.finishLinePositions[rank - 1].transform.position.z), 0.5f).OnComplete(() =>
                    {
                        GetComponent<PlayerMovementTwo>().anim.SetBool("sad", true);
                    }); ;
                    
                    //throw + sad
                   
                }

            }

            if (BotManager.instance.playerFinalPos == 1)
            {
                if (playerType == PlayerType.human && !GameManager.instance.finishCrossed && curStackCount > 0)
                {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.winSFX);
                    StartCoroutine(GameManager.instance.EndBonusPods());
                }
                else if (playerType == PlayerType.human && !GameManager.instance.finishCrossed && curStackCount <= 0)
                {
                    transform.DOMove(new Vector3(GameManager.instance.finishLine.transform.position.x, GameManager.instance.finishLine.transform.position.y + 1f, GameManager.instance.finishLine.transform.position.z), 0.5f).OnComplete(() =>
                    {
                        bonusCoinX = 1;
                        StopPlayerAtEnd();
                    }); ;
                }
            }
            if (BotManager.instance.playerFinalPos > 1)
            {
                if (playerType == PlayerType.human && !GameManager.instance.finishCrossed && curStackCount > 0)
                {
                    cannotBuild = true;
                    GetComponent<PlayerMovementTwo>().enabled = false;
                    GetComponent<Rigidbody>().useGravity = false;
                    GetComponent<Rigidbody>().isKinematic = true;
                    GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                    //throw stack then sad
                    transform.DOMove(new Vector3(GameManager.instance.finishLine.transform.position.x, GameManager.instance.finishLine.transform.position.y + 1f, GameManager.instance.finishLine.transform.position.z), 0.5f).OnComplete(() =>
                    {
                        ThrowStack();
                        bonusCoinX = 1;
                        StopPlayerAtEnd();
                    });
                }
                else if (playerType == PlayerType.human && !GameManager.instance.finishCrossed && curStackCount <= 0)
                {
                    GetComponent<PlayerMovementTwo>().enabled = false;
                    GetComponent<Rigidbody>().useGravity = false;
                    GetComponent<Rigidbody>().isKinematic = true;
                    GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                    transform.DOMove(new Vector3(GameManager.instance.finishLine.transform.position.x, GameManager.instance.finishLine.transform.position.y + 1f, GameManager.instance.finishLine.transform.position.z), 0.5f).OnComplete(() =>
                    {
                        bonusCoinX = 1;
                        StopPlayerAtEnd();
                    });
                }
            }

            GameManager.instance.finishCrossed = true;
            GameObject fx = Instantiate(GameManager.instance.confettiFX, other.transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("water") && !GameManager.instance.dead)
        {
            water = false;
            windFx.SetActive(false);
            //boostFX.SetActive(false);
        }
            
    }
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("bot"))
        {
            other.transform.GetComponent<PathCreation.Examples.PathFollower>().enabled = false;
            other.transform.DOMoveY(other.transform.position.y + 5, 3f);
            other.transform.DOMoveZ(other.transform.position.z + 15, 3f);
            //kill bot

        }
        if (other.gameObject.CompareTag("ground"))
        {
            grounded = true;
            jumping = false;
            transform.GetComponent<PlayerMovementTwo>().anim.SetBool("jump", false);
            canPlaceLog = false;
            transform.GetComponent<PlayerMovementTwo>().speed = 7;
            bouncing = false;
            windFx.SetActive(false);
            
            if (playerType == PlayerType.human)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.landSFX);
                HapticPatterns.PlayConstant(0.2f, 0f, 0.15f);
                boostFX.SetActive(false);
            }
              
        }
        if (other.gameObject.CompareTag("logPlaced"))
        {
            transform.GetComponent<PlayerMovementTwo>().speed = 11;
            windFx.SetActive(true);
        }
        if (other.gameObject.CompareTag("logPlacedOld"))
        {
            transform.GetComponent<PlayerMovementTwo>().speed = 11;
            windFx.SetActive(true);

            grounded = true;
            jumping = false;
            transform.GetComponent<PlayerMovementTwo>().anim.SetBool("jump", false);
            canPlaceLog = false;
            bouncing = false;
        }
        if (other.gameObject.CompareTag("endpod") && !GameManager.instance.dead && !endPodReached)
        {
            if(curStackCount <= 0)
            {
                //stop player control
                lastPodOn = other.gameObject;
                endPodReached = true;
                transform.DOMove(new Vector3(lastPodOn.transform.position.x, lastPodOn.transform.position.y + 1.2f, lastPodOn.transform.position.z), 0.5f).OnComplete(() =>
                {
                    bonusCoinX = lastPodOn.GetComponent<EndPod>().endPodID;
                    StopPlayerAtEnd();
                    //transform.DOMoveY(lastPodOn.transform.position.y + 0.3f, 0.1f);
                });
                
                UIManager.instance.panelGame.SetActive(false);
                GameObject fx = Instantiate(GameManager.instance.confettiFX, other.transform.position, Quaternion.identity);
                Destroy(fx, 2f);
                other.gameObject.transform.GetChild(1).gameObject.SetActive(true);
                other.gameObject.transform.GetChild(1).GetComponent<ParticleSystem>().loop = true;
                other.gameObject.transform.GetChild(1).GetComponent<ParticleSystem>().startColor= other.gameObject.GetComponent<MeshRenderer>().material.color;
                SoundManager.Instance.PlaySFX(SoundManager.Instance.winPodSFX);
            }
            else
            {
                lastPodOn = other.gameObject;
                //effects
                HapticPatterns.PlayConstant(0.2f, 0f, 0.15f);
                other.gameObject.transform.GetChild(1).gameObject.SetActive(true);
                other.gameObject.transform.GetChild(1).GetComponent<ParticleSystem>().startColor = other.gameObject.GetComponent<MeshRenderer>().material.color;
                SoundManager.Instance.PlaySFX(SoundManager.Instance.winPodSFX);
            }

        }

    }
    private void OnCollisionExit(Collision other)
    {
        if (other.gameObject.CompareTag("ground") && curStackCount <= 0 && !GameManager.instance.dead)
        {
            if (playerType == PlayerType.human)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.jumpSFX);
                boostFX.SetActive(false);
            }
               
            transform.DOMoveY(transform.position.y + 7f, 0.75f).SetLoops(2,LoopType.Yoyo);
            jumping = true;
            bouncing = false;
            transform.GetComponent<PlayerMovementTwo>().anim.SetBool("jump", true);
            grounded = false;
            windFx.SetActive(false);
            
        }
        if (other.gameObject.CompareTag("ground") && curStackCount > 0)
        {
            canPlaceLog = true;
            logSpawnDelay = 0.125f;
            grounded = false;
        }
    }
    public void PlaceLog()
    {
        if (curStackCount > 0 && !cannotBuild)
        {
            GameObject go = Instantiate(GameManager.instance.logPlaceObj, new Vector3(transform.position.x, 0f, transform.position.z + 0.3f), transform.rotation);
            go.transform.DOMoveY(go.transform.position.y - 0.2f, 0.05f);
            //go.transform.DOLocalRotateQuaternion(Quaternion.Euler(0, 0, 0), 0.2f);
            go.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0.3f), 0.2f);
            StartCoroutine(LogTagRoutine(go));
            Destroy(logs[curStackCount - 1]);
            logs.RemoveAt(curStackCount - 1);
            curStackCount--;

            if (water)
                transform.DOMoveY(0.1f,0.01f);

            if (playerType == PlayerType.human)
            {
                //UIManager.instance.txtLogCount.transform.DOMoveY(UIManager.instance.txtLogCount.transform.position.y - 0.005f * curStackCount, 0.1f);
                SoundManager.Instance.PlaySFX(SoundManager.Instance.logPlaceSFX);
                HapticPatterns.PlayConstant(0.07f, 0f, 0.1f);
                brickPlaced++;
                if(brickPlaced >= 8)
                {
                    boostFX.SetActive(true);
                    transform.GetComponent<PlayerMovementTwo>().speed = 13;
                    brickPlaced = 0;
                }
            }
            if (playerType == PlayerType.bot)
            {
                go.transform.GetComponent<MeshRenderer>().material = GameManager.instance.podMats[randStackColor];
            }
            GameObject fx = Instantiate(GameManager.instance.stackFX, go.transform.position, Quaternion.identity);
            Destroy(fx, 1f);
            
            //transform.GetComponent<PlayerMovement>().anim.SetTrigger("place");

            logSpawnDelay = 0;
            if(curStackCount <= 0)
            {
                if (playerType == PlayerType.human)
                {
                    SoundManager.Instance.PlaySFX(SoundManager.Instance.jumpSFX);
                    boostFX.SetActive(false);
                }
                    
                windFx.SetActive(false);               
                transform.DOMoveY(transform.position.y + 7f, 0.75f).SetLoops(2, LoopType.Yoyo);
                jumping = true;
                bouncing = false;
                grounded = false;
                transform.GetComponent<PlayerMovementTwo>().anim.SetBool("carry", false);
                transform.GetComponent<PlayerMovementTwo>().anim.SetBool("jump", true);
                transform.GetComponent<PlayerMovementTwo>().speed = 7;
            }
        }
    }
    public IEnumerator DeathRoutine()
    {

        yield return new WaitForSeconds(1.5f);
        UIManager.instance.panelGameOver.SetActive(true);
        SoundManager.Instance.PlaySFX(SoundManager.Instance.loseSFX);
    }
    void StopPlayerAtEnd()
    {
        if (!playerEndStopped)
        {
            string sufix = BotManager.instance.playerFinalPos == 1 ? "st" : BotManager.instance.playerFinalPos == 2 ? "nd" : BotManager.instance.playerFinalPos == 3 ? "rd" : "th";
            UIManager.instance.txtFinalPos.text = BotManager.instance.playerFinalPos.ToString() + sufix;

            windFx.SetActive(false);
            if (playerType == PlayerType.human)
                boostFX.SetActive(false);
            GetComponent<PlayerMovementTwo>().anim.SetBool("jump", false);
            if (BotManager.instance.playerFinalPos > 1)
                GetComponent<PlayerMovementTwo>().anim.SetBool("sad", true);
            if (BotManager.instance.playerFinalPos == 1)
                GetComponent<PlayerMovementTwo>().anim.SetBool("dance", true);
            GetComponent<PlayerMovementTwo>().enabled = false;
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            this.enabled = false;
            Camera.main.transform.parent = null;
            Camera.main.transform.DOLookAt(this.transform.position, 0.1f);
            transform.DORotateQuaternion(Quaternion.Euler(0, 180, 0), 0.2f);
            //rotate
            UIManager.instance.panelGame.SetActive(false);
            UIManager.instance.panelGameWin.SetActive(true);
            UIManager.instance.txtCoinGained.text = (100 * bonusCoinX).ToString();
            GameManager.instance.totalCoin += (100 * bonusCoinX);
            PlayerPrefs.SetInt(GameManager.instance.totalCoinKey, GameManager.instance.totalCoin);
            SoundManager.Instance.PlaySFX(SoundManager.Instance.winSFX);
            HapticPatterns.PlayConstant(0.4f, 0f, 0.2f);
            playerEndStopped = true;
        }
    }
    public void GetDistanceFromFinish()
    {
        distFromEnd = Vector3.Distance(transform.position, GameManager.instance.finishLine.transform.position);
        if (playerType == PlayerType.human)
        {
            BotManager.instance.leaderboardDist[0] = distFromEnd;
            rank = BotManager.instance.playerPos;
        }         
        if (playerType == PlayerType.bot)
        {
            BotManager.instance.leaderboardDist[botID + 1] = distFromEnd;
            rank = BotManager.instance.leaderboardDist.IndexOf(BotManager.instance.bots[botID + 1].distFromEnd);
        }

        if (rank == 1)
            crown.SetActive(true);
        else
            crown.SetActive(false);
    }
    public void ThrowStack()
    {
        //stackPos.transform.parent = null;
        //stackPos.transform.DOMoveZ(stackPos.transform.position.z + 100,5f);
        //for (int i = 0; i < curStackCount; i++)
        //{
        //    stackPos.GetChild(0).GetChild(i).gameObject.AddComponent<Rigidbody>();
        //    stackPos.GetChild(0).GetChild(i).gameObject.AddComponent<BoxCollider>();
        //    stackPos.GetChild(0).GetChild(i).parent = null;

        //}
        stackPos.gameObject.SetActive(false);

        curStackCount = 0;
    }
    IEnumerator LogTagRoutine(GameObject other)
    {
        yield return new WaitForSeconds(1f);
        other.tag = "logPlacedOld";
    }
    void CamPosAdjust()
    {
        if (GameManager.instance.player.GetComponent<PlayerCollisions>().curStackCount >= 10)
        {
            cam.transform.position = new Vector3(cam.transform.position.x, Mathf.Lerp(cam.transform.position.y, 11, 0.25f), cam.transform.position.z);
        }
        if (GameManager.instance.player.GetComponent<PlayerCollisions>().curStackCount >= 20)
        {
            cam.transform.position = new Vector3(cam.transform.position.x, Mathf.Lerp(cam.transform.position.y, 12, 0.25f), cam.transform.position.z);
        }
        if (GameManager.instance.player.GetComponent<PlayerCollisions>().curStackCount >= 40)
        {
            cam.transform.position = new Vector3(cam.transform.position.x, Mathf.Lerp(cam.transform.position.y, 13, 0.25f), cam.transform.position.z);
        }
        if (GameManager.instance.player.GetComponent<PlayerCollisions>().curStackCount < 10)
        {
            cam.transform.position = new Vector3(cam.transform.position.x, Mathf.Lerp(cam.transform.position.y, 10, 0.25f), cam.transform.position.z);
        }
    }
}
