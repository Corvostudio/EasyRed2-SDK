using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;

public class ModUtils
{

    //paths
    public static string XML_FILE_NAME = "index.xml";
    public static readonly int COVER_RESOLUTION = 512;
    

    public static string GetDocumentPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/ER2 TOOLS/Export"; } }
    public static string GetModsRootPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/ER2 TOOLS"; } }
    
    public static string GetCoverPhotoPath(string bundleName) {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/ER2 TOOLS/Export/" + bundleName + "/cover.jpg";
    }

    public static string GetExportedModPath(string mod_bundle_name)
    {
        string path = GetDocumentPath + "/" + mod_bundle_name;
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }


    //codice

    public static Texture2D GetCoverPhotoImage(string bundleName)
    {
        string path = GetCoverPhotoPath(bundleName);
        if (!File.Exists(path))
        {
            //OP SE NON LO TROVA
            SaveTextureToJPG(Resources.Load<Texture2D>("cover"), GetExportedModPath(bundleName), "cover", COVER_RESOLUTION, COVER_RESOLUTION);
        }
        return LoadTextureFromFile(path);
    }

    public static Texture2D LoadTextureFromFile(string path)
    {
        byte[] photoBytes = System.IO.File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(COVER_RESOLUTION, COVER_RESOLUTION);
        tex.LoadImage(photoBytes);
        return tex;
    }

    public static bool HasXML(string mod_bunle_name)
    {
        return File.Exists(GetDocumentPath+"/" + mod_bunle_name + "/" + XML_FILE_NAME);
    }

    /// <summary>
    /// Returns the content of the xml
    /// </summary>
    public static string GetXMLContentsFromFile(string xml_path, string filename)
    {
        string XMLPath = xml_path + "/" + filename;
        if (!string.IsNullOrEmpty(XMLPath))
            return File.ReadAllText(XMLPath);

        return "error";
    }

    /// <summary>
    /// Save content of XML to file
    /// </summary>
    public static void SaveXmlToFile(string xml, string xml_path, string filename)
    {
        xml_path +="/"+ filename;

        if (string.IsNullOrEmpty(xml_path))
            return;

        //Debug.Log(path);
        File.WriteAllText(xml_path, xml);
    }


    //parsare le info di pubblicazione workshop
    public static void ParseWorkshopInfoXml(string xmlString,out string bundleName, out ModVisibility visibilityOption, out string changelogs)
    {
        // Load XML string into an XML document
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlString);

        //load metadata
        bundleName = xmlDoc.SelectSingleNode("/WorkshopMetadata/BundleName").InnerText;
        visibilityOption=(ModVisibility)int.Parse(xmlDoc.SelectSingleNode("/WorkshopMetadata/ModVisibility").InnerText);
        changelogs = xmlDoc.SelectSingleNode("/WorkshopMetadata/Changelogs").InnerText;
    }
    public static string CreateWorkshopInfoXml(string bundleName, ModVisibility visibilityOption, string changelogs)
    {
        //make sure characters like < > , . " = ? are not used in any of the strings


        // Create XML document
        XmlDocument xmlDoc = new XmlDocument();
        XmlElement mainRoot = xmlDoc.CreateElement("WorkshopMetadata");

        #region METADATA
        XmlElement bundleNameElement = xmlDoc.CreateElement("BundleName");
        bundleNameElement.InnerText = bundleName;
        mainRoot.AppendChild(bundleNameElement);

        XmlElement visibility = xmlDoc.CreateElement("ModVisibility");
        visibility.InnerText = ((int)visibilityOption).ToString();
        mainRoot.AppendChild(visibility);

        XmlElement changelogsElement = xmlDoc.CreateElement("Changelogs");
        changelogsElement.InnerText = changelogs;
        mainRoot.AppendChild(changelogsElement);
        #endregion

        // Return XML string representation
        xmlDoc.AppendChild(mainRoot);
        return xmlDoc.OuterXml;
    }




    public static void SaveTextureToJPG(Texture2D texture, string folderPath, string fileName, int width, int height)
    {
        // Create the target directory if it doesn't exist
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Resize the texture to the desired resolution
        Texture2D resizedTexture = ScaleTexture(texture, width, height);

        // Convert the resized texture to bytes
        byte[] bytes = resizedTexture.EncodeToJPG();

        // Create the file path
        string filePath = Path.Combine(folderPath, fileName + ".jpg");

        // Write the bytes to the file
        File.WriteAllBytes(filePath, bytes);

        // Destroy the temporary resized texture
        UnityEngine.Object.DestroyImmediate(resizedTexture);

        //Debug.Log("Texture saved to: " + filePath);
    }

    private static Texture2D ScaleTexture(Texture2D sourceTexture, int targetWidth, int targetHeight)
    {
        // Create a new texture with the desired resolution
        Texture2D resizedTexture = new Texture2D(targetWidth, targetHeight);

        // Scale and set the pixels from the source texture to the resized texture
        Color[] pixels = resizedTexture.GetPixels(0);
        float incX = (1.0f / (float)targetWidth);
        float incY = (1.0f / (float)targetHeight);
        for (int y = 0; y < targetHeight; ++y)
        {
            for (int x = 0; x < targetWidth; ++x)
            {
                pixels[(y * targetWidth) + x] = sourceTexture.GetPixelBilinear(incX * ((float)x + 0.5f), incY * ((float)y + 0.5f));
            }
        }
        resizedTexture.SetPixels(pixels, 0);
        resizedTexture.Apply();

        return resizedTexture;
    }





    private static string TryGetNodeText(XmlDocument xmlDoc, string path)
    {
        XmlNode node = xmlDoc.SelectSingleNode(path);
        return node != null ? node.InnerText : "";
    }
    //parsare l'xml con i metadati della mod
    public static XmlParsingResult ParseXmlString(string xmlString)
    {
        XmlParsingResult ret = new XmlParsingResult();

        // Load XML string into an XML document
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlString);


        //load metadata
        ret.userName = xmlDoc.SelectSingleNode("/ModData/Metadata/UserName").InnerText;
        if (ulong.TryParse(xmlDoc.SelectSingleNode("/ModData/Metadata/UserSteamID").InnerText,out ulong result))
            ret.workshopFileId = result;
        else
            ret.workshopFileId = 0;
        ret.modName = TryGetNodeText(xmlDoc,"/ModData/Metadata/ModName");
        ret.modDescription = xmlDoc.SelectSingleNode("/ModData/Metadata/ModDescription").InnerText;
        ret.bundleName = xmlDoc.SelectSingleNode("/ModData/Metadata/BundleName").InnerText;

        // Get item elements
        XmlNodeList itemNodes = xmlDoc.SelectNodes("/ModData/Prefabs/RegisteredPrefab");

        // Iterate through item elements
        for (int i = 0; i < itemNodes.Count; i++)
        {
            XmlNode itemNode = itemNodes[i];

            // Get value elements
            XmlNode value1Node = itemNode.SelectSingleNode("PrefabName");
            XmlNode value2Node = itemNode.SelectSingleNode("DisplayName");
            XmlNode value3Node = itemNode.SelectSingleNode("Type");

            // Restore values to arrays
            ret.prefab_name.Add(value1Node.InnerText);
            ret.display_name.Add(value2Node.InnerText);
            ret.prefab_prop_type.Add((ModPropType)int.Parse(value3Node.InnerText));


            /*restoredArray1[i] = value1Node.InnerText;
            restoredArray2[i] = value2Node.InnerText;
            restoredArray3[i] = value3Node.InnerText;*/
        }
        
        
        /*XmlNodeList ammoNodes = xmlDoc.SelectNodes("/ModData/AmmoTypeList");
        for (int j = 0; j < ammoNodes.Count; j++)
        {
            BulletData bd = new BulletData();

            XmlNode ammoNode = ammoNodes[j];
            
            // Get value elements
            XmlNode value1Node = ammoNode.SelectSingleNode("Speed");
            XmlNode value2Node = ammoNode.SelectSingleNode("ShellType");
            XmlNode value3Node = ammoNode.SelectSingleNode("AutoDestroyDelay"); 
            XmlNode value4Node = ammoNode.SelectSingleNode("Caliber");
            XmlNode value5Node = ammoNode.SelectSingleNode("mmPenetration");
            XmlNode value6Node = ammoNode.SelectSingleNode("ShellPrefab"); 
            XmlNode value7Node = ammoNode.SelectSingleNode("BulletBehaviour");
            XmlNode value8Node = ammoNode.SelectSingleNode("ExplosionRadius");
            XmlNode value9Node = ammoNode.SelectSingleNode("ExplosionMaxDamage");
            XmlNode value10Node = ammoNode.SelectSingleNode("ExplosionPenetration");
            XmlNode value11Node = ammoNode.SelectSingleNode("ExplosionParticlePrefab");

            bd.speed = int.Parse(value1Node.InnerText);
            bd.shellType = (ShellType)int.Parse(value2Node.InnerText);
            bd.autoDestroy_delay = float.Parse(value3Node.InnerText);
            bd.caliber = float.Parse(value4Node.InnerText);
            bd.mmPenetration = int.Parse(value5Node.InnerText);
            bd.shell_prefab = value6Node.InnerText;
            bd.behaviour = (BulletBehaviour)int.Parse(value7Node.InnerText);
            bd.explosion_radius = float.Parse(value8Node.InnerText);
            bd.explosion_maxDamage = float.Parse(value9Node.InnerText);
            bd.explosion_penetration = float.Parse(value10Node.InnerText);
            bd.explosion_particle_prefab = value11Node.InnerText;


            ret.ammoTypes.Add(bd);
        }*/

        return ret;
    }



    public static string CreateXmlString(XmlParsingResult xmlData)
    {
        // Create XML document
        XmlDocument xmlDoc = new XmlDocument();
        XmlElement mainRoot = xmlDoc.CreateElement("ModData");

        #region METADATA
        XmlElement metadataRoot = xmlDoc.CreateElement("Metadata");
        XmlElement userNameMD = xmlDoc.CreateElement("UserName");
        userNameMD.InnerText = xmlData.userName;
        metadataRoot.AppendChild(userNameMD);

        XmlElement userSteamIdMD = xmlDoc.CreateElement("UserSteamID");
        userSteamIdMD.InnerText = xmlData.workshopFileId.ToString();
        metadataRoot.AppendChild(userSteamIdMD);

        XmlElement modDescriptionMD = xmlDoc.CreateElement("ModDescription");
        modDescriptionMD.InnerText = xmlData.modDescription;
        metadataRoot.AppendChild(modDescriptionMD);

        XmlElement modnameMD = xmlDoc.CreateElement("ModName");//use rinput
        modnameMD.InnerText = xmlData.modName;
        metadataRoot.AppendChild(modnameMD);

        XmlElement bundleNameMD = xmlDoc.CreateElement("BundleName");
        bundleNameMD.InnerText = xmlData.bundleName;
        metadataRoot.AppendChild(bundleNameMD);

        mainRoot.AppendChild(metadataRoot);
        #endregion

        #region MOD ITEMS
        // Create root element
        XmlElement prefabsRoot = xmlDoc.CreateElement("Prefabs");


        // Iterate through the arrays
        for (int i = 0; i < xmlData.prefab_name.Count; i++)
        {
            // Create item element
            XmlElement itemElement = xmlDoc.CreateElement("RegisteredPrefab");

            // Create value elements
            XmlElement value1Element = xmlDoc.CreateElement("PrefabName");
            value1Element.InnerText = xmlData.prefab_name[i];
            itemElement.AppendChild(value1Element);

            XmlElement value2Element = xmlDoc.CreateElement("DisplayName");
            value2Element.InnerText = xmlData.display_name[i];
            itemElement.AppendChild(value2Element);

            XmlElement value3Element = xmlDoc.CreateElement("Type");
            value3Element.InnerText = ((int)xmlData.prefab_prop_type[i]).ToString();
            itemElement.AppendChild(value3Element);

            // Append item element to the root
            prefabsRoot.AppendChild(itemElement);
        }

        // Append root element to the XML document
        mainRoot.AppendChild(prefabsRoot);
        #endregion



        /*#region AMMO TYPES
        // Create root element
        XmlElement ammoTypesRoot = xmlDoc.CreateElement("AmmoTypeList");

        //FOREACH AMMO TYPE -> REGISTER IT
        Debug.Log(xmlData.ammoTypes.Count);
        foreach (BulletData ammo in xmlData.ammoTypes)
        {
            // Create item element
            XmlElement itemElement = xmlDoc.CreateElement("AmmoType");


            XmlElement value1Element = xmlDoc.CreateElement("Speed");
            value1Element.InnerText = ammo.speed.ToString();
            itemElement.AppendChild(value1Element);
            XmlElement value2Element = xmlDoc.CreateElement("ShellType");
            value2Element.InnerText = ((int)ammo.shellType).ToString();
            itemElement.AppendChild(value2Element);
            XmlElement value3Element = xmlDoc.CreateElement("AutoDestroyDelay");
            value3Element.InnerText = ammo.autoDestroy_delay.ToString();
            itemElement.AppendChild(value3Element);
            XmlElement value4Element = xmlDoc.CreateElement("Caliber");
            value4Element.InnerText = ammo.caliber.ToString();
            itemElement.AppendChild(value4Element);
            XmlElement value5Element = xmlDoc.CreateElement("mmPenetration");
            value5Element.InnerText = ammo.mmPenetration.ToString();
            itemElement.AppendChild(value5Element);
            XmlElement value6Element = xmlDoc.CreateElement("ShellPrefab");
            value6Element.InnerText = ammo.shell_prefab;
            itemElement.AppendChild(value6Element);
            XmlElement value7Element = xmlDoc.CreateElement("BulletBehaviour");
            value7Element.InnerText = ((int)ammo.behaviour).ToString();
            itemElement.AppendChild(value7Element);
            XmlElement value8Element = xmlDoc.CreateElement("ExplosionRadius");
            value8Element.InnerText = ammo.explosion_radius.ToString();
            itemElement.AppendChild(value8Element);
            XmlElement value9Element = xmlDoc.CreateElement("ExplosionMaxDamage");
            value9Element.InnerText = ammo.explosion_maxDamage.ToString();
            itemElement.AppendChild(value9Element);
            XmlElement value10Element = xmlDoc.CreateElement("ExplosionPenetration");
            value10Element.InnerText = ammo.explosion_penetration.ToString(); 
            itemElement.AppendChild(value10Element);
            XmlElement value11Element = xmlDoc.CreateElement("ExplosionParticlePrefab");
            value11Element.InnerText = ammo.explosion_particle_prefab;
            itemElement.AppendChild(value11Element);

            ammoTypesRoot.AppendChild(itemElement);
        }

        mainRoot.AppendChild(ammoTypesRoot);
        #endregion*/


        // Return XML string representation
        xmlDoc.AppendChild(mainRoot);
        return xmlDoc.OuterXml;
    }

    public static string GenerateModItemId(string bundlename, string prefabname)
    {
        if (!string.IsNullOrEmpty(bundlename))
            return bundlename + "_" + prefabname;
        return prefabname;
    }
}

public class XmlParsingResult
{
    public string userName = "Unknown";
    public ulong workshopFileId = 0;
    public string modName = "Test";
    public string bundleName = "Test";
    public string modDescription = "No mod description";

    public List<string> prefab_name = new List<string>();
    public List<string> display_name = new List<string>();
    public List<ModPropType> prefab_prop_type = new List<ModPropType>();


    //LISTA DI AMMO ITEM 
    //public List<BulletData> ammoTypes= new List<BulletData>();

    public void Clear()
    {
        prefab_name.Clear();
        display_name.Clear();
        prefab_prop_type.Clear();
        //ammoTypes.Clear();
    }
}

public enum ModPropType
{
    buildings = 0,
    props,
    vehicles,
    items,
    weapons,
    ammo,
    attachment,
    uniforms,
    sounds,
    terrain_textures,
    terrain_details,
    factions,
    lut_texture,
    crosshair_texture,
    props_huge
}

public enum ModVisibility
{
    Private = 0,
    FriendsOnly=1,
    Public=2,
    Unlisted=3
}