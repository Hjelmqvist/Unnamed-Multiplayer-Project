using UnityEngine;

public class ModelMover : MonoBehaviour
{
    [SerializeField] MovePoint[] movePoints;
    [SerializeField] MoveType moveType;
    [SerializeField] int startIndex = 0;

    [System.Serializable]
    struct MovePoint
    {
        public Vector3 positionOffset;
        public float time;
    }

    enum MoveType
    {
        BackAndForth,
        Loop
    }

    Vector3 startPosition;
    Vector3 fromPosition;
    Vector3 toPosition;

    MovePoint currentPoint;
    int currentIndex = 0;
    float currentTime = 0;

    // Used for MoveType.BackAndForth
    bool goingBack = false;

    private void Start()
    {
        // Set starting values so we have something to go from.
        startPosition = transform.position;
        currentPoint = movePoints[currentIndex];
        SetTargetPoint(startIndex + 1);
    }

    void Update()
    {
        // Move over two positions over set amount of time.
        currentTime = Mathf.Clamp(currentTime + Time.deltaTime, 0, currentPoint.time);
        transform.position = Vector3.Lerp(fromPosition, toPosition, currentTime / currentPoint.time);

        if (currentTime >= currentPoint.time)
        {
            // Set which point to go to next.
            SetNextPoint();
        }
    }

    /// <summary>
    /// Sets the next move point depending on selected move type.
    /// </summary>
    private void SetNextPoint()
    {
        int nextIndex = currentIndex + 1;
        switch (moveType)
        {
            case MoveType.BackAndForth:
                if (!goingBack && currentIndex + 1 >= movePoints.Length)
                    goingBack = true;
                else if (goingBack && currentIndex - 1 < 0)
                    goingBack = false;
                nextIndex = currentIndex + (goingBack ? -1 : 1);
                break;

            case MoveType.Loop:
                if (currentIndex + 1 >= movePoints.Length)
                    nextIndex = 0;
                break;
        }
        SetTargetPoint(nextIndex);
    }

    /// <summary>
    /// Sets the current point and relevant values.
    /// </summary>
    private void SetTargetPoint(int index)
    {
        fromPosition = startPosition + currentPoint.positionOffset;
        currentIndex = index;
        currentPoint = movePoints[currentIndex];
        currentTime = 0;
        toPosition = startPosition + currentPoint.positionOffset;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            startPosition = transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        if (movePoints.Length == 0)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(startPosition, transform.localScale);

        for (int i = 0; i < movePoints.Length - 1; i++)
        {
            Vector3 fromPosition = startPosition + movePoints[i].positionOffset;
            Vector3 toPosition = startPosition + movePoints[i + 1].positionOffset;

            // Draw line between points.
            Gizmos.DrawLine(fromPosition, toPosition);

            // Draw a cube at the next position with this objects scale.
            Gizmos.DrawWireCube(toPosition, transform.localScale);
        }

        // If set to move in a loop draw an extra line between first and last positions.
        if (moveType.Equals(MoveType.Loop))
        {
            Gizmos.DrawLine(startPosition, startPosition + movePoints[movePoints.Length - 1].positionOffset);

            // Draw sphere at current target position
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(toPosition, 0.5f);
            }
        }
    }
}