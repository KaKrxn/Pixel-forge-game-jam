using UnityEngine;

public sealed class CustomerAgent : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform outsideDoorPoint;
    [SerializeField] private Transform insideDoorPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 0.05f;
    [SerializeField] private Bubble bubble;
    [SerializeField] private CustomerLayer customerLayer;
    [SerializeField] private Door door;
    [Header("Audio")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip footstepLoopClip;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 1f;
    [SerializeField, Min(0.01f)] private float footstepStepDistance = 0.45f;
    [SerializeField, Min(0f)] private float footstepMinInterval = 0.12f;

    private GameFlow flow;
    private EnterStep enterStep = EnterStep.None;
    private ExitStep exitStep = ExitStep.None;
    private float footstepDistanceAccumulator;
    private float footstepCooldown;

    private enum EnterStep
    {
        None,
        WalkingToOutsideDoor,
        WalkingToInsideDoor,
        WalkingToCounter
    }

    private enum ExitStep
    {
        None,
        WalkingToInsideDoor,
        WalkingToOutsideDoor,
        WalkingToExit
    }

    private void Awake()
    {
        ResolveFootstepSource();
        HideBubble();
    }

    private void Update()
    {
        if (enterStep != EnterStep.None)
        {
            MoveAlongEnterPath();
            return;
        }

        if (exitStep != ExitStep.None)
        {
            MoveAlongExitPath();
        }
    }

    public void BeginEnter(GameFlow ownerFlow)
    {
        flow = ownerFlow;
        exitStep = ExitStep.None;

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position;
        }

        if (customerLayer == null)
        {
            customerLayer = GetComponent<CustomerLayer>();
        }

        customerLayer?.SetOutsideLayer();
        door?.Close();
        gameObject.SetActive(true);
        HideBubble();
        StartFootsteps();
        enterStep = outsideDoorPoint != null ? EnterStep.WalkingToOutsideDoor : EnterStep.WalkingToCounter;
    }

    public void ConfigurePath(
        Transform nextSpawnPoint,
        Transform nextOutsideDoorPoint,
        Transform nextInsideDoorPoint,
        Transform nextCounterPoint,
        Transform nextExitPoint)
    {
        spawnPoint = nextSpawnPoint;
        outsideDoorPoint = nextOutsideDoorPoint;
        insideDoorPoint = nextInsideDoorPoint;
        counterPoint = nextCounterPoint;
        exitPoint = nextExitPoint;
    }

    public void ConfigureBubble(Bubble nextBubble)
    {
        bubble = nextBubble;
        HideBubble();
    }

    public void ConfigureDoor(Door nextDoor)
    {
        door = nextDoor;
    }

    public void BeginExit()
    {
        enterStep = EnterStep.None;
        HideBubble();
        door?.Close();
        StartFootsteps();
        exitStep = insideDoorPoint != null ? ExitStep.WalkingToInsideDoor : ExitStep.WalkingToExit;
    }

    public void HideBubble()
    {
        if (bubble != null)
        {
            bubble.Hide();
        }
    }

    private void MoveAlongEnterPath()
    {
        Transform target = GetCurrentTarget();
        if (target == null)
        {
            AdvanceEnterStep();
            return;
        }

        Vector3 beforePosition = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        TickFootsteps(Vector3.Distance(beforePosition, transform.position));

        if (Vector3.Distance(transform.position, target.position) <= stopDistance)
        {
            AdvanceEnterStep();
        }
    }

    private void MoveAlongExitPath()
    {
        Transform target = GetCurrentExitTarget();
        if (target == null)
        {
            AdvanceExitStep();
            return;
        }

        Vector3 beforePosition = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        TickFootsteps(Vector3.Distance(beforePosition, transform.position));

        if (Vector3.Distance(transform.position, target.position) <= stopDistance)
        {
            AdvanceExitStep();
        }
    }

    private void ArriveAtCounter()
    {
        enterStep = EnterStep.None;
        StopFootsteps();

        if (bubble != null)
        {
            bubble.Show(() => flow.BeginDialog(this));
        }

        flow.CustomerReachedCounter(this);
    }

    private Transform GetCurrentTarget()
    {
        return enterStep switch
        {
            EnterStep.WalkingToOutsideDoor => outsideDoorPoint,
            EnterStep.WalkingToInsideDoor => insideDoorPoint,
            EnterStep.WalkingToCounter => counterPoint,
            _ => null
        };
    }

    private Transform GetCurrentExitTarget()
    {
        return exitStep switch
        {
            ExitStep.WalkingToInsideDoor => insideDoorPoint,
            ExitStep.WalkingToOutsideDoor => outsideDoorPoint,
            ExitStep.WalkingToExit => exitPoint != null ? exitPoint : spawnPoint,
            _ => null
        };
    }

    private void AdvanceEnterStep()
    {
        switch (enterStep)
        {
            case EnterStep.WalkingToOutsideDoor:
                door?.Open();
                customerLayer?.SetInsideLayer();
                enterStep = insideDoorPoint != null ? EnterStep.WalkingToInsideDoor : EnterStep.WalkingToCounter;
                break;

            case EnterStep.WalkingToInsideDoor:
                door?.Close();
                enterStep = EnterStep.WalkingToCounter;
                break;

            case EnterStep.WalkingToCounter:
                ArriveAtCounter();
                break;

            default:
                enterStep = EnterStep.None;
                break;
        }
    }

    private void AdvanceExitStep()
    {
        switch (exitStep)
        {
            case ExitStep.WalkingToInsideDoor:
                door?.Open();
                customerLayer?.SetOutsideLayer();
                exitStep = outsideDoorPoint != null ? ExitStep.WalkingToOutsideDoor : ExitStep.WalkingToExit;
                break;

            case ExitStep.WalkingToOutsideDoor:
                door?.Close();
                exitStep = ExitStep.WalkingToExit;
                break;

            case ExitStep.WalkingToExit:
                FinishExit();
                break;

            default:
                exitStep = ExitStep.None;
                break;
        }
    }

    private void FinishExit()
    {
        exitStep = ExitStep.None;
        StopFootsteps();
        flow?.CustomerLeft(this);
    }

    private void ResolveFootstepSource()
    {
        if (footstepSource == null)
        {
            footstepSource = GetComponent<AudioSource>();
        }
    }

    private void StartFootsteps()
    {
        ResolveFootstepSource();
        if (footstepSource == null && footstepLoopClip != null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
        }

        if (footstepSource == null || footstepLoopClip == null)
        {
            return;
        }

        footstepSource.loop = false;
        footstepSource.volume = footstepVolume;
        footstepDistanceAccumulator = footstepStepDistance;
        footstepCooldown = 0f;
    }

    private void TickFootsteps(float movedDistance)
    {
        if (movedDistance <= 0.0001f || footstepLoopClip == null)
        {
            return;
        }

        ResolveFootstepSource();
        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
        }

        footstepCooldown = Mathf.Max(0f, footstepCooldown - Time.deltaTime);
        footstepDistanceAccumulator += movedDistance;

        if (footstepDistanceAccumulator < footstepStepDistance || footstepCooldown > 0f)
        {
            return;
        }

        footstepDistanceAccumulator = 0f;
        footstepCooldown = footstepMinInterval;
        footstepSource.PlayOneShot(footstepLoopClip, footstepVolume);
    }

    private void StopFootsteps()
    {
        if (footstepSource != null && footstepSource.isPlaying)
        {
            footstepSource.Stop();
        }
    }
}
