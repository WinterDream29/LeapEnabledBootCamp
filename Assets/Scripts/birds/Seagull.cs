using UnityEngine;
using System.Collections;

[System.Serializable]
public partial class Seagull : MonoBehaviour
{
    public AudioClip[] sounds;
    public float soundFrequency;
    public float animationSpeed;
    public float minSpeed;
    public float turnSpeed;
    public float randomFreq;
    public float randomForce;
    public float toOriginForce;
    public float toOriginRange;
    public float damping;
    public float gravity;
    public float avoidanceRadius;
    public float avoidanceForce;
    public float followVelocity;
    public float followRadius;
    public float bankTurn;
    public bool raycast;
    public float bounce;
    private SeagullFlightPath target;
    private Transform origin;
    private Vector3 velocity;
    private Vector3 normalizedVelocity;
    private Vector3 randomPush;
    private Vector3 originPush;
    private Vector3 gravPush;
    private RaycastHit hit;
    private Transform[] objects;
    private Seagull[] otherSeagulls;
    private Animation animationComponent;
    private Transform transformComponent;
    private bool gliding;
    private float bank;
    private AnimationState glide;
    private bool paused;
    public virtual void Start()
    {
        this.randomFreq = 1f / this.randomFreq;
        this.paused = false;
        this.gameObject.tag = this.transform.parent.gameObject.tag;
        this.animationComponent = (Animation) this.GetComponentInChildren(typeof(Animation));
        this.animationComponent["Take 001"].speed = this.animationSpeed;
        this.animationComponent.Blend("Take 001");
        this.animationComponent["Take 001"].normalizedTime = Random.value;
        this.glide = this.animationComponent["Take 001"];
        this.origin = this.transform.parent;
        this.target = (SeagullFlightPath) this.origin.GetComponent(typeof(SeagullFlightPath));
        this.transform.parent = null;
        this.transformComponent = this.transform;
        Component[] tempSeagulls = new Component[0];
        if (this.transform.parent)
        {
            tempSeagulls = this.transform.parent.GetComponentsInChildren(typeof(Seagull));
        }
        this.objects = new Transform[tempSeagulls.Length];
        this.otherSeagulls = new Seagull[tempSeagulls.Length];
        int i = 0;
        while (i < tempSeagulls.Length)
        {
            this.objects[i] = tempSeagulls[i].transform;
            this.otherSeagulls[i] = tempSeagulls[i];
            i++;
        }
        this.StartCoroutine(this.UpdateRandom());
    }

    public virtual IEnumerator UpdateRandom()
    {
        while (true)
        {
            this.randomPush = Random.insideUnitSphere * this.randomForce;
            yield return new WaitForSeconds(this.randomFreq + Random.Range(-this.randomFreq / 2, this.randomFreq / 2));
        }
    }

    public virtual void Update()
    {
        if (this.origin == null)
        {
            UnityEngine.Object.Destroy(this.gameObject);
            return;
        }
        if (GameManager.pause)
        {
            if (!this.paused)
            {
                this.paused = true;
                this.animationComponent.Stop();
            }
            return;
        }
        else
        {
            if (this.paused)
            {
                this.paused = false;
                this.animationComponent.Blend("Take 001");
            }
        }
        float speed = this.velocity.magnitude;
        Vector3 avoidPush = Vector3.zero;
        Vector3 avgPoint = Vector3.zero;
        int count = 0;
        float f = 0f;
        Vector3 myPosition = this.transformComponent.position;
        int i = 0;
        while (i < this.objects.Length)
        {
            Transform o = this.objects[i];
            if (o != this.transformComponent)
            {
                Vector3 otherPosition = o.position;
                avgPoint = avgPoint + otherPosition;
                count++;
                Vector3 forceV = myPosition - otherPosition;
                float d = forceV.magnitude;
                if (d < this.followRadius)
                {
                    if (d < this.avoidanceRadius)
                    {
                        f = 1f - (d / this.avoidanceRadius);
                        if (d > 0)
                        {
                            avoidPush = avoidPush + (((forceV / d) * f) * this.avoidanceForce);
                        }
                    }
                    f = d / this.followRadius;
                    Seagull otherSealgull = this.otherSeagulls[i];
                    avoidPush = avoidPush + ((otherSealgull.normalizedVelocity * f) * this.followVelocity);
                }
            }
            i++;
        }
        if (count > 0)
        {
            avoidPush = avoidPush / count;
            Vector3 toAvg = (avgPoint / count) - myPosition;
        }
        else
        {
            toAvg = Vector3.zero;
        }
        if (this.target != null)
        {
            forceV = (this.origin.position + this.target.offset) - myPosition;
        }
        else
        {
            forceV = this.origin.position - myPosition;
        }
        d = forceV.magnitude;
        f = d / this.toOriginRange;
        if (d > 0)
        {
            this.originPush = ((forceV / d) * f) * this.toOriginForce;
        }
        if ((speed < this.minSpeed) && (speed > 0))
        {
            this.velocity = (this.velocity / speed) * this.minSpeed;
        }
        Vector3 wantedVel = this.velocity;
        wantedVel = wantedVel - ((wantedVel * this.damping) * Time.deltaTime);
        wantedVel = wantedVel + (this.randomPush * Time.deltaTime);
        wantedVel = wantedVel + (this.originPush * Time.deltaTime);
        wantedVel = wantedVel + (avoidPush * Time.deltaTime);
        wantedVel = wantedVel + ((toAvg.normalized * this.gravity) * Time.deltaTime);
        Vector3 diff = this.transformComponent.InverseTransformDirection(wantedVel - this.velocity).normalized;
        this.bank = Mathf.Lerp(this.bank, diff.x, Time.deltaTime * 0.8f);
        this.velocity = Vector3.RotateTowards(this.velocity, wantedVel, this.turnSpeed * Time.deltaTime, 100f);
        this.transformComponent.rotation = Quaternion.FromToRotation(Vector3.right, this.velocity);
        //transformComponent.Rotate(0, 0, -bank * bankTurn);
        // Raycast
        float distance = speed * Time.deltaTime;
        if ((this.raycast && (distance > 0f)) && Physics.Raycast(myPosition, this.velocity, out this.hit, distance))
        {
            this.velocity = Vector3.Reflect(this.velocity, this.hit.normal) * this.bounce;
        }
        else
        {
            this.transformComponent.Translate(this.velocity * Time.deltaTime, Space.World);
        }
        // Sounds
        if (!(this.sounds == null))
        {
            if (this.sounds.Length > 0)
            {
                if (SeagullSoundHeat.heat < Mathf.Pow(Random.value, (1 / this.soundFrequency) / Time.deltaTime))
                {
                    AudioSource.PlayClipAtPoint(this.sounds[Random.Range(0, this.sounds.Length)], myPosition, 0.9f);
                    SeagullSoundHeat.heat = SeagullSoundHeat.heat + ((1 / this.soundFrequency) / 10);
                }
            }
        }
        this.normalizedVelocity = this.velocity.normalized;
    }

    public Seagull()
    {
        this.sounds = new AudioClip[0];
        this.soundFrequency = 1f;
        this.animationSpeed = 1f;
        this.bounce = 0.8f;
    }

}