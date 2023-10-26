# Conduit

Conduit is a productivity-enhancing framework within Voice SDK that extracts metadata from an app’s codebase at edit time to help streamline the development process, as well as to speed up dispatching during runtime. It generates a manifest file from the app that captures details of the relevant components needed to understand the voice activation structures within that app. This manifest file is then used to help dispatch incoming voice requests to the right callbacks.

# Benefits to Using Conduit

There are a number of benefits to using Conduit instead of the traditional approach to handling the metadata. These include:

* You don't need to manually register your callbacks. You can simply tag them with the `MatchIntent` attribute.

* Callbacks are strongly typed. You don't need to parse Wit Response modules to get the values of the roles. You can just write your method accepting the parameters needed. For example, `SetVolume(int level)` automatically gets the `int` value for the level role supplied when a Set Volume request is made.

* Enumerations are automatically matched to entity types. For example, if you have `ChangeColor(Shape shape, Color color)` where `Shape` and `Color` are enums, those values will be automatically resolved as well, provided they’ve been trained on Wit.ai.

* Improved performance over non-Conduit `MatchIntent`. When generating the manifest files, Conduit only uses reflection on the specific assemblies you tag with the `ConduitAssembly` attribute, rather than all available assemblies. This can result in up to a 90x faster initialization time. Additionally, during runtime, the dispatcher does not need to scan for the callbacks, as that information has already been captured in the manifest file.

* You can use static and instance methods as callbacks.

* Conduit is backwards compatible with existing `MatchIntent` attributes. In most cases, existing code will continue to work in the same way. For more information on this, see the **Current Limitations** section below.

# To use Conduit in your app

1. In the Unity editor, go to **Oculus > Voice SDK > Settings**, and select the *Use Conduit* option to enable Conduit.
![image](Images/UseConduitCheckbox.png)
2. Annotate your callback methods with the `MatchIntent` attribute.

3. Annotate the assemblies that contain your callbacks with the `ConduitAssembly` attribute. Conduit will skip all assemblies that are not annotated. This can be done by adding the following code to the `AssemblyInfo.cs` file (or another code file in the assembly that contains your callbacks): `[assembly:ConduitAssembly]`

# Current Limitations

Conduit is currently in a beta version and, because of this, some functionality may not work as expected. If needed, you can disable Conduit in the **Voice SDK Settings** window to return your project to its prior state.

There are several known limitations at this time:

1. At the present time, only primitive types and enums are supported. In the meantime, you can get a Wit.ai **Response** module parameter in addition to the strongly-typed parameters by declaring it as one of the parameters.

2. While Conduit is backwards-compatible with legacy callback methods, the opposite is not true. A callback signature designed for Conduit (and which has a different parameter) will not work if you disable Conduit. If you expect to be switching frequently between Conduit and legacy matching, you may want to use the `ConduitAction` attribute instead.

3. Conduit attributes do not currently fully support multiple attributes on one callback method. While it should still invoke correctly, only the first attribute will currently be used for other metadata.

# Best Practices for Using Conduit to Design Voice SDK Callbacks

When designing the ontology of an app, consider how the flow will work end-to-end. The callback methods should be designed to map to the Wit.ai intents. While different structures may work, some will provide better accuracy and easier maintenance. Below are a few design tips to optimize your code for use with Voice SDK.

1. Avoid using string parameters where possible. When entities are well-defined, create an enum with the list of potential options: For example, rather than using this:

```
public void DropItem(string myItem)
```
Try this instead:

```
public void DropItem(ItemType myItem)
```
2. When intents are similar, consider unifying them and factoring out the concepts that vary in the entities they operate on, and then fan out from your callout in order to execute the logic you want. For example, rather than using this:

```
public void CastFireBall()
public void CastLightningBolt()
```
Try this instead:

```
enum SpellType {Fireball, LightningBolt}
public void CastSpell(SpellType mySpell)
```
3. Instead of using `OnResponse` and `OnPartialResponse` events to assign callbacks, try annotating your callbacks directly in the code using Conduit’s `MatchIntent` attribute. This can reduce maintenance overhead when you change the method’s name or signature. It can also allow you more flexibility in entity resolution.

4. Avoid manually parsing `WitResponseNode` unless absolutely necessary. If you just need to extract parameters, Conduit can automatically parse them for you. In this way, you can keep your code clear and concise should you make changes to your intents or slots. For example, instead of this:

```
public void CastSpell(WitResponseNode node)
{
    // Logic to extract and parse into SpellType.
    // Use SpellType here.
}
```
Try this:

```
public void CastSpell(SpellType mySpell)
{
    // Use SpellType directly.
}
```
5. Avoid using string literals for finite entity values when possible. For example, rather than checking that the value of the direction slot is right, compare it directly to the enum value `Direction.Right`. This can make the code less error-prone and easier to maintain.

6. Avoid manually synchronizing Wit.Ai entity values with your code. Conduit’s Auto Sync feature will automatically provide a sync whenever you need synchronization. This can reduce the chances of human error, as well as saving time.

7. Use the **Specify Assemblies** functionality in Conduit to exclude unneeded assemblies whenever possible. Fewer assemblies will usually improve runtime performance as well as reducing the chance conflicts.

8. When starting off, using the **Relaxed Resolution** mode in Conduit may help you get up to speed more quickly. However, when possible, it’s best to disable this mode. Doing so will usually result in slightly improved runtime performance, as well as fewer false positives.
