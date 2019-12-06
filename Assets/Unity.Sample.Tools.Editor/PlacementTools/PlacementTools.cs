using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class PlacementTools
{
    static PlacementTools()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static Ray lastMouseRay;
    static void OnSceneGUI(SceneView sceneview)
    {
        lastMouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
    }

    [MenuItem("A2/Hotkeys/Position under mouse %#q")]
    static void MousePlace()
    {
        var transforms = Selection.transforms;
        var hit = FindClosestObjectUnderMouseNotSelected();
        
        // Consider selection as a whole to move if the PivotMode is set to SelectionCenter.
        if (Tools.pivotMode == PivotMode.Center && transforms.Length > 1)
        {
            var startHit =FindClosestColliderDownNotSelected(); //Cast a ray from the center of the selection(At highest bound height) going doing, first hit point will be used to align the selection.
            var offset=hit.point-startHit.point;
            foreach (var myTransform in transforms)
            {
                Undo.RegisterCompleteObjectUndo(Selection.transforms, "MousePlace");
                myTransform.position += offset;
            }
            return;
        }
        
        //Default MousePlaceBehaviour(Single selection)
        foreach (var myTransform in transforms)
        {
            Undo.RegisterCompleteObjectUndo(Selection.transforms, "MousePlace");
            myTransform.position = hit.point;
        }
    }

    static RaycastHit FindClosestObjectUnderMouseNotSelected()
    {
        var transforms = Selection.transforms;
        // Get current physicsScene
        var physicsScene = transforms[0].gameObject.scene.GetPhysicsScene();
        
        // Find closest object under mouse, not selected
        RaycastHit[] hits=new RaycastHit[60]; //JulienH: Arbitrary amount of max hits.
        physicsScene.Raycast(lastMouseRay.origin,lastMouseRay.direction,hits);
        
        RaycastHit hit = new RaycastHit();
        var closest_dist = float.MaxValue;
        foreach (var h in hits)
        {
            if (h.collider == null)
                break;
            
            var skipit = false;
            foreach (var t in transforms)
            {
                if (h.collider.transform.IsChildOf(t))
                {
                    skipit = true;
                    break;
                }
            }
            if (skipit)
                continue;
            if (h.distance < closest_dist)
            {
                hit = h;
                closest_dist = h.distance;
            }
        }
        return hit;
    }
    static RaycastHit FindClosestColliderDownNotSelected()
    {
        var transforms = Selection.transforms;
        var gameObjects = Selection.gameObjects;
        var bounds = GetBounds(gameObjects);
        var boundsMaxY = bounds.max.y;
        var maxYCenterPosition = new Vector3(Tools.handlePosition.x,boundsMaxY, Tools.handlePosition.z); //Raycast will start from the center of the selection but at the max height of the selection.
        // Get current physicsScene
        var physicsScene = transforms[0].gameObject.scene.GetPhysicsScene();
        
        // Find closest object under mouse, not selected
        RaycastHit[] hits=new RaycastHit[90]; //JulienH: Arbitrary amount of max hits.
        physicsScene.Raycast(maxYCenterPosition,Vector3.down,hits);
        
        RaycastHit hit = new RaycastHit();
        var closest_dist = float.MaxValue;
        foreach (var h in hits)
        {
            if (h.collider == null)
                break;
            
            var skipit = false;
            foreach (var t in transforms)
            {
                if (h.collider.transform.IsChildOf(t))
                {
                    skipit = true;
                    break;
                }
                if (IsDuplicateRecursive(h.collider.transform,t))
                {
                    skipit = true;
                    break;
                }
            }
            if (skipit)
                continue;
            if (h.distance < closest_dist)
            {
                hit = h;
                closest_dist = h.distance;
            }
        }

        

        if (hit.point.y < bounds.min.y-0.1f) //If the hit is outside of bounds(extended by 10cm), use the bound's min Y as anchor Y position.
        {
            var modifiedHit = new RaycastHit()
            {
                point=new Vector3(hit.point.x,bounds.min.y,hit.point.z),
                normal=Vector3.up,
                distance = hit.distance,
                barycentricCoordinate = hit.barycentricCoordinate
            };
            return modifiedHit;
        }
        return hit;
    }

    [MenuItem("A2/Hotkeys/Align and position under mouse %#z")]
    static void MousePlaceAndAlign()
    {
        var transforms = Selection.transforms;
        if (transforms.Length == 0)
            return;

        var hit = FindClosestObjectUnderMouseNotSelected();

        if (hit.distance == 0)
            return;

        if (Tools.pivotMode == PivotMode.Center && transforms.Length > 1) // Consider selection as a whole to move if the PivotMode is set to SelectionCenter.
        {
            var startHit =FindClosestColliderDownNotSelected(); //Cast a ray from the center of the selection(At highest bound height) going doing, first hit point will be used to align the selection.
            var offset=hit.point-startHit.point;
            var angleOffset=-Vector3.Angle(startHit.normal,hit.normal);
            //Create temporary parent
            var tempParent=new GameObject("TempParent");
            Undo.RegisterCreatedObjectUndo(tempParent, "MousePlace");
            var tempParentTransform = tempParent.transform;
            tempParentTransform.position = startHit.point;
            var originalParentDict=new Dictionary<Transform,Transform>();
            //Set all selection as child of tempParent;
            foreach (var myTransform in transforms)
            {
                originalParentDict.Add(myTransform,myTransform.parent); // save parent of all GO.
                Undo.RegisterCompleteObjectUndo(Selection.transforms, "MousePlace");
                Undo.SetTransformParent(myTransform,tempParentTransform,"MousePlace");
            }

            //Move and rotate tempParent
            tempParentTransform.position = hit.point;
            tempParentTransform.rotation = Quaternion.FromToRotation(startHit.normal, hit.normal); //TODO: Better alignmentneeded.
            
            //Revert parenting
            foreach (var myTransform in transforms)
            {
                Undo.SetTransformParent(myTransform,originalParentDict[myTransform],"MousePlace");
            }
            //Delete tempParent
            Undo.DestroyObjectImmediate(tempParent);
            return;
        }

        foreach (var myTransform in transforms)
        {
            Undo.RegisterCompleteObjectUndo(Selection.transforms, "MousePlaceAndAlign");

            myTransform.position = hit.point;

            // Decide what is most up
            var xdot = Vector3.Dot(myTransform.right, Vector3.up);
            var ydot = Vector3.Dot(myTransform.up, Vector3.up);
            if (Mathf.Abs(xdot) > 0.7f)
            {
                var rot = Quaternion.FromToRotation(myTransform.right, hit.normal);
                myTransform.rotation = rot * myTransform.rotation;
            }
            else if (Mathf.Abs(ydot) > 0.7f)
            {
                var rot = Quaternion.FromToRotation(myTransform.up, hit.normal);
                myTransform.rotation = rot * myTransform.rotation;
            }
            else
            {
                var rot = Quaternion.FromToRotation(myTransform.forward, hit.normal);
                myTransform.rotation = rot * myTransform.rotation;
            }
        }
    }
    
    
    public static Bounds GetBounds(GameObject[] gameObjects)
    {
        Bounds bounds=new Bounds();
        foreach (var gameObject in gameObjects)
        {
            if(bounds.extents.x==0)
                bounds=GetBounds(gameObject);
            else
                bounds.Encapsulate(GetBounds(gameObject));
        }

        return bounds;
    }
    public static Bounds GetBounds(GameObject gameObject)
    {
        Bounds bounds = new Bounds();
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            //Find first enabled renderer to start encapsulate from it
            foreach (Renderer renderer in renderers)
            {
                if (renderer.enabled)
                {
                    bounds = renderer.bounds;
                    break;
                }
            }

            //Encapsulate for all renderers
            foreach (Renderer renderer in renderers)
            {
                if (renderer.enabled)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }
        return bounds;
    }

    private static bool IsDuplicateRecursive(Transform mainTransform, Transform compareTransform)
    {
        if (mainTransform.position == compareTransform.position && mainTransform.rotation == compareTransform.rotation)
        {
            GameObject mainGO = mainTransform.gameObject;
            GameObject compareGO = compareTransform.gameObject;
            MeshCollider mainCollider = mainGO.GetComponent<MeshCollider>();
            MeshCollider compareCollider = compareGO.GetComponent<MeshCollider>();
            if (mainCollider != null && compareCollider != null &&
                mainCollider.sharedMesh == compareCollider.sharedMesh)
            {
                Debug.Log("DuplicateFound");
                return true;

            }
        }

        foreach (Transform childTransform in compareTransform)
        {
            if (IsDuplicateRecursive(mainTransform, childTransform))
                return true;
        }

        return false;
    }
}
