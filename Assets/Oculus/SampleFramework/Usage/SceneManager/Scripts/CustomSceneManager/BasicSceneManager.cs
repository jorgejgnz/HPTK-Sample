using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// This sample shows you how to implement a scene manager with the following features:
///   * Fetch all room scene anchors
///   * Fetch all child scene anchors of a room
///   * Set the location, and name of the object as label
///   * Spawn primitive geometry to match the scene anchor's plane, volume or mesh data
///
/// There is a fallback for running scene capture if no rooms were found.
/// </summary>
public class BasicSceneManager : MonoBehaviour
{
    void Start()
    {
        SceneManagerHelper.RequestScenePermission();

        LoadSceneAsync();
    }

    async void LoadSceneAsync()
    {
        // fetch all rooms, with a SceneCapture fallback
        var rooms = new List<OVRAnchor>();
        await OVRAnchor.FetchAnchorsAsync<OVRRoomLayout>(rooms);
        if (rooms.Count == 0)
        {
            var sceneCaptured = await SceneManagerHelper.RequestSceneCapture();
            if (!sceneCaptured)
                return;

            await OVRAnchor.FetchAnchorsAsync<OVRRoomLayout>(rooms);
        }

        // fetch room elements, create objects for them
        var tasks = rooms.Select(async room =>
        {
            var roomObject = new GameObject($"Room-{room.Uuid}");
            if (!room.TryGetComponent(out OVRAnchorContainer container))
                return;

            var children = new List<OVRAnchor>();
            await container.FetchChildrenAsync(children);
            await CreateSceneAnchors(roomObject, children);
        }).ToList();
        await Task.WhenAll(tasks);
    }

    async Task CreateSceneAnchors(GameObject roomGameObject, List<OVRAnchor> anchors)
    {
        // we create tasks to iterate all anchors in parallel
        var tasks = anchors.Select(async anchor =>
        {
            // can we locate it in the world?
            if (!anchor.TryGetComponent(out OVRLocatable locatable))
                return;
            await locatable.SetEnabledAsync(true);

            // get semantic classification for object name
            var label = "other";
            if (anchor.TryGetComponent(out OVRSemanticLabels labels))
                label = labels.Labels;

            // create and parent Unity game object
            var gameObject = new GameObject(label);
            gameObject.transform.SetParent(roomGameObject.transform);

            // set location and create objects for 2D, 3D, triangle mesh
            var helper = new SceneManagerHelper(gameObject);
            helper.SetLocation(locatable);

            if (anchor.TryGetComponent(out OVRBounded2D b2d) && b2d.IsEnabled)
                helper.CreatePlane(b2d);
            if (anchor.TryGetComponent(out OVRBounded3D b3d) && b3d.IsEnabled)
                helper.CreateVolume(b3d);
        }).ToList();

        await Task.WhenAll(tasks);
    }
}
