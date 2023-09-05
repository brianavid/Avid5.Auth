using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Web;
using System.Runtime.InteropServices;
using NLog;

/// <summary>
/// A class of configuration values helds in a manually edited XML file
/// </summary>
public static class Config
{
    static Logger logger = LogManager.GetCurrentClassLogger();

    public static void Initialize(string configPath)
    {
        ConfigPath = configPath;

        logger.Info($"ConfigPath = {ConfigPath}");
    }

    public static string? ConfigPath { get; private set; }

    static XDocument? doc = null;

    /// <summary>
    /// The XML document
    /// </summary>
    static XDocument? Doc
    {
        get
        {
            if (doc == null)
            {
                var configPath = ConfigPath;
                if (configPath != null)
                {
                    logger.Info($"Load {configPath}");
                    doc = XDocument.Load(configPath);
                }
            }
            return doc;
        }
    }

    /// <summary>
    /// A valid RedirectURI registered for the app in https://developer.spotify.com/
    /// </summary>
    public static string? RedirectUri
    {
        get
        {
            XElement? elAddr = Doc?.Root?.Element("RedirectUri");
            return elAddr?.Value;
        }
    }

    /// <summary>
    /// The Client ID allocated by Spotify for the app
    /// </summary>
    public static string? ClientId
    {
        get
        {
            XElement? elAddr = Doc?.Root?.Element("ClientId");
            return elAddr?.Value;
        }
    }

    /// <summary>
    /// The Client Secret allocated by Spotify for the app
    /// </summary>
    public static string? ClientSecret
    {
        get
        {
            XElement? elAddr = Doc?.Root?.Element("ClientSecret");
            return elAddr?.Value;
        }
    }

    /// <summary>
    /// The fixed Spotify account URL for access via OAUTH - "https://accounts.spotify.com/api/token"
    /// </summary>
    public static string? SpotifyUrl
    {
        get
        {
            XElement? elAddr = Doc?.Root?.Element("SpotifyUrl");
            return elAddr?.Value;
        }
    }
}