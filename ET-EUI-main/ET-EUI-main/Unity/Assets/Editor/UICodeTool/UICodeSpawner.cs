using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public partial class UICodeSpawner
{
	static public void SpawnEUICode(GameObject gameObject)
	{
		if (null == gameObject)
		{
			Debug.LogError("UICode Select GameObject is null!");
			return;
		}

		try
		{
			string uiName = gameObject.name;
			if (uiName.StartsWith(UIPanelPrefix))
			{
				Debug.LogWarning($"----------开始生成UI：{uiName} 相关代码 ----------");
				SpawnUICode(gameObject);
				Debug.LogWarning($"生成UI{uiName} 完毕!!!");
				return;
			}
			Debug.LogError($"选择的预设物不属于 UI, 子UI，滚动列表项，请检查 {uiName}！！！！！！");
		}
		finally
		{
			Path2WidgetCachedDict?.Clear();
			Path2WidgetCachedDict = null;
		}
	}
	
	
    static public void SpawnUICode(GameObject gameObject)
    {
	    Path2WidgetCachedDict?.Clear();
        Path2WidgetCachedDict = new Dictionary<string, List<Component>>();
        
		FindAllWidgets(gameObject.transform, "");
		
        SpawnCodeForUI(gameObject);
        SpawnCodeForUIEventHandle(gameObject);
        SpawnCodeForUIModel(gameObject);
        
        SpawnCodeForUIBehaviour(gameObject);
        SpawnCodeForUIComponentBehaviour(gameObject);
        
        AssetDatabase.Refresh();
    }
    
    static void SpawnCodeForUI(GameObject gameObject)
    {
        string strUIName  = gameObject.name;
        string strFilePath = Application.dataPath + "/../Codes/HotfixView/Demo/UI/" + strUIName ;
        
        
        if ( !Directory.Exists(strFilePath) )
        {
	        Directory.CreateDirectory(strFilePath);
        }
        
	    strFilePath = Application.dataPath + "/../Codes/HotfixView/Demo/UI/" + strUIName + "/" + strUIName + "System.cs";
        if(File.Exists(strFilePath))
        {
            Debug.LogError("已存在 " + strUIName + "System.cs,将不会再次生成。");
            return;
        }

        StreamWriter sw = new StreamWriter(strFilePath, false, Encoding.UTF8);
        StringBuilder strBuilder = new StringBuilder();
        strBuilder.AppendLine("using System.Collections;")
                  .AppendLine("using System.Collections.Generic;")
                  .AppendLine("using System;")
                  .AppendLine("using UnityEngine;")
                  .AppendLine("using UnityEngine.UI;\r\n");

        strBuilder.AppendLine("namespace ET");
        strBuilder.AppendLine("{");
        
        strBuilder.AppendFormat("\t[FriendClass(typeof({0}))]\r\n", strUIName);
       
        strBuilder.AppendFormat("\tpublic static  class {0}\r\n", strUIName + "System");
          strBuilder.AppendLine("\t{");
          strBuilder.AppendLine("");


        strBuilder.AppendFormat("\t\tpublic static void RegisterUIEvent(this {0} self)\n",strUIName)
               .AppendLine("\t\t{")
               .AppendLine("\t\t ")
               .AppendLine("\t\t}")
               .AppendLine();


        strBuilder.AppendFormat("\t\tpublic static void ShowWindow(this {0} self, Entity contextData = null)\n", strUIName);
        strBuilder.AppendLine("\t\t{");
          
        strBuilder.AppendLine("\t\t}")
	        .AppendLine();
        
        strBuilder.AppendLine("\t\t \r\n");
        
        strBuilder.AppendLine("\t}");
        strBuilder.AppendLine("}");

        sw.Write(strBuilder);
        sw.Flush();
        sw.Close();
    }


    /// <summary>
    /// 自动生成WindowId代码
    /// </summary>
    /// <param name="gameObject"></param>
    static void SpawnWindowIdCode(GameObject gameObject)
    {
	    string strUIName = gameObject.name;
	    string strFilePath = Application.dataPath + "/../Codes/ModelView/Module/UI/WindowId.cs" ;
	    
	    if(!File.Exists(strFilePath))
	    {
		    Debug.LogError(" 当前不存在WindowId.cs!!!");
		    return;
	    }
	    
	    string originWindowIdContent = File.ReadAllText(strFilePath);
	    if (originWindowIdContent.Contains(strUIName.Substring(2)))
	    {
			return;
	    }
	    int windowIdEndIndex   = GetWindowIdEndIndex(originWindowIdContent);
	    originWindowIdContent  = originWindowIdContent.Insert(windowIdEndIndex, "\tWindowID_"+strUIName.Substring(2) + ",\n\t");
	    File.WriteAllText(strFilePath, originWindowIdContent);
    }
    
    public static int GetWindowIdEndIndex(string content)
    {
	    Regex regex = new Regex("WindowID");
	    Match match = regex.Match(content);
	    Regex regex1 = new Regex("}");
	    MatchCollection matchCollection = regex1.Matches(content);
	    for (int i = 0; i < matchCollection.Count; i++)
	    {
		    if (matchCollection[i].Index > match.Index)
		    {
			    return matchCollection[i].Index;
		    }
	    }
	    return -1;
    }
    
	static void SpawnCodeForUIEventHandle(GameObject gameObject)
    {
        string strUIName = gameObject.name;
        string strFilePath = Application.dataPath + "/../Codes/HotfixView/Demo/UI/" + strUIName + "/Event" ;
        
        
        if ( !Directory.Exists(strFilePath) )
        {
	        Directory.CreateDirectory(strFilePath);
        }
        
	    strFilePath = Application.dataPath + "/../Codes/HotfixView/Demo/UI/" + strUIName + "/Event/" + strUIName + "EventHandler.cs";
        if(File.Exists(strFilePath))
        {
	        Debug.LogError("已存在 " + strUIName + ".cs,将不会再次生成。");
            return;
        }
        SpawnWindowIdCode(gameObject);
        StreamWriter sw = new StreamWriter(strFilePath, false, Encoding.UTF8);
        StringBuilder strBuilder = new StringBuilder();
        
        strBuilder.AppendLine("namespace ET");
        strBuilder.AppendLine("{");
        strBuilder.AppendLine("\t[FriendClass(typeof(WindowCoreData))]");
        strBuilder.AppendLine("\t[FriendClass(typeof(UIBaseWindow))]");
        strBuilder.AppendFormat("\t[AUIEvent(WindowID.WindowID_{0})]\n",strUIName.Substring(2));
        strBuilder.AppendFormat("\tpublic  class {0}EventHandler : IAUIEventHandler\r\n", strUIName);
          strBuilder.AppendLine("\t{");
          strBuilder.AppendLine("");
          
          
          strBuilder.AppendLine("\t\tpublic void OnInitWindowCoreData(UIBaseWindow uiBaseWindow)")
	          .AppendLine("\t\t{");

          strBuilder.AppendFormat("\t\t  uiBaseWindow.WindowData.windowType = UIWindowType.Normal; \r\n");
          
          strBuilder.AppendLine("\t\t}")
	          .AppendLine();
          
          strBuilder.AppendLine("\t\tpublic void OnInitComponent(UIBaseWindow uiBaseWindow)")
            		.AppendLine("\t\t{");

          strBuilder.AppendFormat("\t\t  uiBaseWindow.AddComponent<{0}ViewComponent>(); \r\n",strUIName);
          strBuilder.AppendFormat("\t\t  uiBaseWindow.AddComponent<{0}>(); \r\n",strUIName);
          
          strBuilder.AppendLine("\t\t}")
            .AppendLine();
          
          strBuilder.AppendLine("\t\tpublic void OnRegisterUIEvent(UIBaseWindow uiBaseWindow)")
	          .AppendLine("\t\t{");

          strBuilder.AppendFormat("\t\t  uiBaseWindow.GetComponent<{0}>().RegisterUIEvent(); \r\n",strUIName);
          
          strBuilder.AppendLine("\t\t}")
	          .AppendLine();
          
          
          strBuilder.AppendLine("\t\tpublic void OnShowWindow(UIBaseWindow uiBaseWindow, Entity contextData = null)")
	          .AppendLine("\t\t{");
          strBuilder.AppendFormat("\t\t  uiBaseWindow.GetComponent<{0}>().ShowWindow(contextData); \r\n",strUIName);
          strBuilder.AppendLine("\t\t}")
	          .AppendLine();

            
          strBuilder.AppendLine("\t\tpublic void OnHideWindow(UIBaseWindow uiBaseWindow)")
	          .AppendLine("\t\t{");
          
          strBuilder.AppendLine("\t\t}")
	          .AppendLine();
          
          
          strBuilder.AppendLine("\t\tpublic void BeforeUnload(UIBaseWindow uiBaseWindow)")
	          .AppendLine("\t\t{");
          
          strBuilder.AppendLine("\t\t}")
	          .AppendLine();
          
        strBuilder.AppendLine("\t}");
        strBuilder.AppendLine("}");

        sw.Write(strBuilder);
        sw.Flush();
        sw.Close();
    }
    
	
	static void SpawnCodeForUIModel(GameObject gameObject)
    {
        string strUIName = gameObject.name;
        string strFilePath = Application.dataPath + "/../Codes/ModelView/Demo/UI/" + strUIName  ;
        
        
        if ( !Directory.Exists(strFilePath) )
        {
	        Directory.CreateDirectory(strFilePath);
        }
        
	    strFilePath = Application.dataPath + "/../Codes/ModelView/Demo/UI/" + strUIName  + "/" + strUIName  + ".cs";
        if(File.Exists(strFilePath))
        {
	        Debug.LogError("已存在 " + strUIName + ".cs,将不会再次生成。");
            return;
        }

        StreamWriter sw = new StreamWriter(strFilePath, false, Encoding.UTF8);
        StringBuilder strBuilder = new StringBuilder();
        
        strBuilder.AppendLine("namespace ET");
        strBuilder.AppendLine("{");
        strBuilder.AppendLine("\t [ComponentOf(typeof(UIBaseWindow))]");
       
        strBuilder.AppendFormat("\tpublic  class {0} :Entity,IAwake,IUILogic\r\n", strUIName);
          strBuilder.AppendLine("\t{");
          strBuilder.AppendLine("");
          
	    strBuilder.AppendLine("\t\tpublic "+strUIName+"ViewComponent View { get => this.Parent.GetComponent<"+ strUIName +"ViewComponent>();} \r\n");
	    
        strBuilder.AppendLine("\t\t \r\n");
        strBuilder.AppendLine("\t}");
        strBuilder.AppendLine("}");

        sw.Write(strBuilder);
        sw.Flush();
        sw.Close();
    }
    

    static void SpawnCodeForUIBehaviour(GameObject gameObject)
    {
        if (null == gameObject)
        {
            return;
        }
        string strUIName = gameObject.name ;
        string strUIComponentName =  gameObject.name + "ViewComponent";

        string strFilePath = Application.dataPath + "/../Codes/HotfixView/Demo/UIBehaviour/" + strUIName;

        if ( !Directory.Exists(strFilePath) )
        {
	        Directory.CreateDirectory(strFilePath);
        }
	    strFilePath = Application.dataPath + "/../Codes/HotfixView/Demo/UIBehaviour/" + strUIName + "/" + strUIComponentName + "System.cs";
	    
        StreamWriter sw = new StreamWriter(strFilePath, false, Encoding.UTF8);

        
        StringBuilder strBuilder = new StringBuilder();
        strBuilder.AppendLine()
	        .AppendLine("using UnityEngine;");
        strBuilder.AppendLine("using UnityEngine.UI;");
        strBuilder.AppendLine("namespace ET");
        strBuilder.AppendLine("{");
        strBuilder.AppendLine("\t[ObjectSystem]");
        strBuilder.AppendFormat("\tpublic class {0}AwakeSystem : AwakeSystem<{1}> \r\n", strUIComponentName, strUIComponentName);
        strBuilder.AppendLine("\t{");
        strBuilder.AppendFormat("\t\tpublic override void Awake({0} self)\n",strUIComponentName);
        strBuilder.AppendLine("\t\t{");
        strBuilder.AppendLine("\t\t\tself.uiTransform = self.GetParent<UIBaseWindow>().uiTransform;");
        strBuilder.AppendLine("\t\t}");
        strBuilder.AppendLine("\t}");
        strBuilder.AppendLine("\n");
        
       
        strBuilder.AppendLine("\t[ObjectSystem]");
        strBuilder.AppendFormat("\tpublic class {0}DestroySystem : DestroySystem<{1}> \r\n", strUIComponentName, strUIComponentName);
        strBuilder.AppendLine("\t{");
        strBuilder.AppendFormat("\t\tpublic override void Destroy({0} self)",strUIComponentName);
        strBuilder.AppendLine("\n\t\t{");
        strBuilder.AppendFormat("\t\t\tself.DestroyWidget();\r\n");
        strBuilder.AppendLine("\t\t}");
        strBuilder.AppendLine("\t}");
        strBuilder.AppendLine("}");
        sw.Write(strBuilder);
        sw.Flush();
        sw.Close();
    }

    static void SpawnCodeForUIComponentBehaviour(GameObject gameObject)
    {
	    if (null == gameObject)
	    {
		    return;
	    }
	    string strUIName = gameObject.name ;
	    string strUIComponentName =  gameObject.name + "ViewComponent";


	    string strFilePath = Application.dataPath + "/../Codes/ModelView/Demo/UIBehaviour/" + strUIName;
	    if ( !Directory.Exists(strFilePath) )
	    {
		    Directory.CreateDirectory(strFilePath);
	    }
	    strFilePath = Application.dataPath + "/../Codes/ModelView/Demo/UIBehaviour/" + strUIName + "/" + strUIComponentName + ".cs";
	    StreamWriter sw = new StreamWriter(strFilePath, false, Encoding.UTF8);
	    StringBuilder strBuilder = new StringBuilder();
	    strBuilder.AppendLine()
		    .AppendLine("using UnityEngine;");
	    strBuilder.AppendLine("using UnityEngine.UI;");
	    strBuilder.AppendLine("namespace ET");
	    strBuilder.AppendLine("{");
	    strBuilder.AppendLine("\t[ComponentOf(typeof(UIBaseWindow))]");
	    strBuilder.AppendLine("\t[EnableMethod]");
	    strBuilder.AppendFormat("\tpublic  class {0} : Entity,IAwake,IDestroy \r\n", strUIComponentName)
		    .AppendLine("\t{");
     
	    CreateWidgetBindCode(ref strBuilder, gameObject.transform);

	    CreateDestroyWidgetCode(ref strBuilder);
	    
	    CreateDeclareCode(ref strBuilder);
	    strBuilder.AppendFormat("\t\tpublic Transform uiTransform = null;\r\n");
	    strBuilder.AppendLine("\t}");
	    strBuilder.AppendLine("}");
        
	    sw.Write(strBuilder);
	    sw.Flush();
	    sw.Close();
    }


    public static void CreateDestroyWidgetCode( ref StringBuilder strBuilder,bool isScrollItem = false)
    {
	    strBuilder.AppendFormat("\t\tpublic void DestroyWidget()");
	    strBuilder.AppendLine("\n\t\t{");
	    CreateUIWidgetDisposeCode(ref strBuilder);
	    strBuilder.AppendFormat("\t\t\tthis.uiTransform = null;\r\n");
	    if (isScrollItem)
	    {
		    strBuilder.AppendLine("\t\t\tthis.DataId = 0;");
	    }
	    strBuilder.AppendLine("\t\t}\n");
    }
    
    
    public static void CreateUIWidgetDisposeCode(ref StringBuilder strBuilder,bool isSelf = false)
    {
	    string pointStr = isSelf ? "self" : "this";
	    foreach (KeyValuePair<string, List<Component>> pair in Path2WidgetCachedDict)
	    {
		    foreach (var info in pair.Value)
		    {
			    Component widget = info;
			    string strClassType = widget.GetType().ToString();
			    
			    string widgetName = widget.name + strClassType.Split('.').ToList().Last();
			    strBuilder.AppendFormat("\t\t	{0}.{1} = null;\r\n", pointStr,widgetName);
		    }
		 
	    }

	 
    }

    public static void CreateWidgetBindCode(ref StringBuilder strBuilder, Transform transRoot)
    {
        foreach (KeyValuePair<string, List<Component>> pair in Path2WidgetCachedDict)
        {
	        foreach (var info in pair.Value)
	        {
		        Component widget = info;
				string strPath = GetWidgetPath(widget.transform, transRoot);
				string strClassType = widget.GetType().ToString();
				string strInterfaceType = strClassType;
				
				string widgetName = widget.name + strClassType.Split('.').ToList().Last();
				
				
				strBuilder.AppendFormat("		public {0} {1}\r\n", strInterfaceType, widgetName);
				strBuilder.AppendLine("     	{");
				strBuilder.AppendLine("     		get");
				strBuilder.AppendLine("     		{");
				
				strBuilder.AppendLine("     			if (this.uiTransform == null)");
				strBuilder.AppendLine("     			{");
				strBuilder.AppendLine("     				Log.Error(\"uiTransform is null.\");");
				strBuilder.AppendLine("     				return null;");
				strBuilder.AppendLine("     			}");

				strBuilder.AppendFormat("     			if( this.{0} == null )\n" , widgetName);
				strBuilder.AppendLine("     			{");
				strBuilder.AppendFormat("		    		this.{0} = UIFindHelper.FindDeepChild<{2}>(this.uiTransform.gameObject,\"{1}\");\r\n", widgetName, strPath, strInterfaceType);
				strBuilder.AppendLine("     			}");
				strBuilder.AppendFormat("     			return this.{0};\n" , widgetName);
				
	            strBuilder.AppendLine("     		}");
	            strBuilder.AppendLine("     	}\n");
	        }
        }
    }
    
    public static void CreateDeclareCode(ref StringBuilder strBuilder)
    {
	    foreach (KeyValuePair<string,List<Component> > pair in Path2WidgetCachedDict)
	    {
		    foreach (var info in pair.Value)
		    {
			    Component widget = info;
			    string strClassType = widget.GetType().ToString();
			    
			    string widgetName = widget.name + strClassType.Split('.').ToList().Last();
			    strBuilder.AppendFormat("\t\tprivate {0} {1} = null;\r\n", strClassType, widgetName);
		    }
		    
	    }
    }

    public static void FindAllWidgets(Transform trans, string strPath)
	{
		if (null == trans)
		{
			return;
		}
		for (int nIndex= 0; nIndex < trans.childCount; ++nIndex)
		{
			Transform child = trans.GetChild(nIndex);
			string strTemp = strPath+"/"+child.name;
			
		
			if (child.name.StartsWith(UIWidgetPrefix))
			{
				foreach (var uiComponent in WidgetInterfaceList)
				{
					Component component = child.GetComponent(uiComponent);
					if (null == component)
					{
						continue;
					}
					
					if ( Path2WidgetCachedDict.ContainsKey(child.name)  )
					{
						Path2WidgetCachedDict[child.name].Add(component);
						continue;
					}
					
					List<Component> componentsList = new List<Component>(); 
					componentsList.Add(component);
					Path2WidgetCachedDict.Add(child.name, componentsList);
				}
			}
			
			FindAllWidgets(child, strTemp);
		}
	}

    static string GetWidgetPath(Transform obj, Transform root)
    {
        string path = obj.name;

        while (obj.parent != null && obj.parent != root)
        {
            obj = obj.transform.parent;
            path = obj.name + "/" + path;
        }
        return path;
    }
    
    static UICodeSpawner()
    {
        WidgetInterfaceList = new List<string>();        
        WidgetInterfaceList.Add("Button");
        WidgetInterfaceList.Add( "Text");
        WidgetInterfaceList.Add("TMPro.TextMeshProUGUI");
        WidgetInterfaceList.Add("Input");
        WidgetInterfaceList.Add("InputField");
        WidgetInterfaceList.Add( "Scrollbar");
        WidgetInterfaceList.Add("ToggleGroup");
        WidgetInterfaceList.Add("Toggle");
        WidgetInterfaceList.Add("Dropdown");
        WidgetInterfaceList.Add("Slider");
        WidgetInterfaceList.Add("ScrollRect");
        WidgetInterfaceList.Add( "Image");
        WidgetInterfaceList.Add("RawImage");
        WidgetInterfaceList.Add("Canvas");
        WidgetInterfaceList.Add("UIWarpContent");
        WidgetInterfaceList.Add("LoopVerticalScrollRect");
        WidgetInterfaceList.Add("LoopHorizontalScrollRect");
        WidgetInterfaceList.Add("UnityEngine.EventSystems.EventTrigger");
    }

    private static Dictionary<string, List<Component> > Path2WidgetCachedDict =null;
    private static List<string> WidgetInterfaceList = null;
    private const string UIPanelPrefix  = "UI";
    private const string UIWidgetPrefix = "m_";
}

