using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterAssembler : MonoBehaviour
{
    public List<GameObject> torsos;
    public List<GameObject> heads;
    
    private GameObject selectedTorso;
    private GameObject selectedHead;

    private void Start()
    {
        int randomTorsoIndex = Random.Range(0, torsos.Count);
        int randomHeadIndex = Random.Range(0, heads.Count);

        GameObject torso = Instantiate(torsos.ElementAt(randomTorsoIndex));

        BodyPartAttacher bpa = torso.GetComponent<BodyPartAttacher>();
        bpa.headObject = Instantiate(heads.ElementAt(randomHeadIndex), bpa.headAttachPoint);
    }
}