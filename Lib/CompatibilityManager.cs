﻿/*
 CustomIconDashboarder - KeePass Plugin to get some information and 
  manage custom icons

 Copyright (C) 2015 Jareth Lomson <incognito1234@users.sourceforge.net>
 
 This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */
 
using System;

using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;

using System.Windows.Forms;
using KeePassLib;

namespace LomsonLib.UI
{
	/// <summary>
	/// Class to manage different version of Keepass.
	/// Since version 2.29, HighDPI custom icons are taken into account.
	/// New methods have appeared and old methods became obsoletes.
	/// This class aims to use new methods when it is possible by avoiding
	/// compilation errors.
	///   
	/// </summary>
	
	/* 
	 * Version 1.2 - 2016/04/04
	 * Get list of standard icons with High Definitions
	 * 
	 * Version 1.1 - 26/10/2015
     * Use ScaleImage in ResizedImage method
	 * 
     * Version 1.0
     * Initial Version
     */

	public class CompatibilityManager
	{
		// KeePass.UI.DpiUtil from version >= 2.27
		private static Type       TYPE_DpiUtil = null;
		
		// KeePass.Properties.Resources all version and internal
		private static Type       TYPE_Properties_Resources = null;
		
		// PwCustomIcon.GetImage from version >= 2.29
		private static MethodInfo METHOD_GetImage_NoParameters  = null;
		private static MethodInfo METHOD_GetImage_TwoParameters = null;
		
		
		// KeePass.UI.DpiUtil.ScaleIntX from version >= 2.27
		// KeePass.UI.DpiUtil.ScaleIntY from version >= 2.27
		private static MethodInfo METHOD_ScaleIntX = null;
		private static MethodInfo METHOD_ScaleIntY = null;
		
		// KeePassLib.Utility.GfxUtil.ScaleImage from version >= 2.29
		private static MethodInfo METHOD_ScaleImage = null;
		
		// KeePass.Properties.Resources.Images_Client_HighRes from version >= 2.29
		// KeePass.Util.Archive.ImageArchive.Load             from version >= 2.29
		// KeePass.Util.Archive.ImageArchive                  from version >= 2.29
		private static Type         TYPE_ImageArchive = null;
		private static object       PROPERTYVALUE_Images_Client_HighRes = null;
		private static MethodInfo   METHOD_ImageArchive_Load  = null;
		private static MethodInfo   METHOD_ImageArchive_GetImages = null;
		
		private static bool IS_INITIALIZED = false;
		
		private CompatibilityManager()
		{
		}
		
		private static void EnsureInitialize() 
		{
			if (! IS_INITIALIZED) {
				METHOD_GetImage_NoParameters = GetMethod(
					typeof(KeePassLib.PwCustomIcon).AssemblyQualifiedName,
					"GetImage",
					new Type[]{ });
				
				METHOD_GetImage_TwoParameters = GetMethod(
					typeof(KeePassLib.PwCustomIcon).AssemblyQualifiedName,
					"GetImage",
					new Type[]{ typeof(int), typeof(int) });
				
				// Get TYPE_DpiUtil and TYPE_Properties_Resources
				foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
		        {
				 	if (a.GetName().Name.Equals("KeePass")){
				 		foreach (var b in a.GetTypes() ) {
				 			if (b.FullName.Equals("KeePass.UI.DpiUtil")) {
								TYPE_DpiUtil = b;
				 			}
							else if (b.FullName.Equals("KeePass.Properties.Resources")) {
							    TYPE_Properties_Resources = b;
							}
							else if (b.FullName.Equals("KeePass.Util.Archive.ImageArchive")) {
							     TYPE_ImageArchive = b;
							}
				 		}
				 		
				 	}
		        }
				Debug.Assert( TYPE_Properties_Resources != null);
				
				// Get ScaleIntX and ScaleIntY
				if (TYPE_DpiUtil != null) {
					METHOD_ScaleIntX = GetMethod(
						TYPE_DpiUtil.AssemblyQualifiedName,
						"ScaleIntX",
						new []{ typeof(int) });
					METHOD_ScaleIntY = GetMethod(
						TYPE_DpiUtil.AssemblyQualifiedName,
						"ScaleIntY",
						new []{ typeof(int) });
				}
				
				
				// Get ScaleImage
				METHOD_ScaleImage = GetMethod(
					typeof(KeePassLib.Utility.GfxUtil).AssemblyQualifiedName,
					"ScaleImage",
					new Type[]{ typeof(System.Drawing.Image), typeof(int), typeof(int) });
				
				// Get Image_Client property Info
				PROPERTYVALUE_Images_Client_HighRes = GetPropertyValue(
					TYPE_Properties_Resources,
					"Images_Client_HighRes");
				
				// Method Load of Image Archiver
				if (TYPE_ImageArchive != null) {
					METHOD_ImageArchive_Load = GetMethod(
						TYPE_ImageArchive.AssemblyQualifiedName,
						"Load", 
						new Type[] {typeof( byte[] )}
					);
					METHOD_ImageArchive_GetImages = GetMethod(
						TYPE_ImageArchive.AssemblyQualifiedName,
						"GetImages",
						new Type[] {typeof(int), typeof(int), typeof(bool) }
					);
				}
				
				IS_INITIALIZED = true;
			}
			
			AppDomain.CurrentDomain.GetAssemblies();
		}
		
		/// <summary>
		/// Get Method from class Name, methodName and parameters
		/// </summary>
		/// <param name="className">Class Name</param>
		/// <param name="methodName">Methode Name</param>
		/// <param name="parameters">Type arrays of parameters</param>
		/// <returns></returns>
		private static MethodInfo GetMethod(string className, string methodName, Type[]parameters)
		{
			MethodInfo result;
			try {
		    	var type = Type.GetType(className,false,false);
		    	result = type.GetMethod(methodName, parameters );
			}
			catch (System.Reflection.AmbiguousMatchException) {
				result= null;
			}
		    return result;
		}
		
		/// <summary>
		/// Get Property value from type and propertyName
		/// </summary>
		/// <param name="type">type</param>
		/// <param name="propertyName">Property Name</param>
		/// <returns></returns>
		private static object GetPropertyValue(Type type, string propertyName)
		{
			object result=null;
			try {
				PropertyInfo prop = type.GetProperty(propertyName,
				                                    BindingFlags.Static |
				                                    BindingFlags.NonPublic);
			
				if (prop!=null) {
					result = prop.GetValue(null,null);
				}
			}
			catch (System.Reflection.AmbiguousMatchException) {
				result= null;
			}
		    return result;
		}
		
		/// <summary>
		/// Return KeePass.UI.DpiUtil.ScaleIntX if exists (keepass >= 2.28).
		/// i if it does not exits
		/// NB: KeePass.UI.DpiUtil exists from keepass >= 2.27
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static int ScaleIntX(int i)
		{
			EnsureInitialize();
			
			int result=-1;
			
			try{
				
				if (METHOD_ScaleIntX != null) {
					result = (int)METHOD_ScaleIntX.Invoke(null, new Object[]{i});
				}
				else {
					result = i;
				}
			}
			catch (Exception e) {
				Debug.Assert(false, "Error with ScaleIntX: " + e.ToString());
				result= -1;
			}
		    return result;
		}
		
		/// <summary>
		/// Return KeePass.UI.DpiUtil.ScaleIntY if exists (keepass >= 2.28).
		/// i if it does not exits
		/// NB: KeePass.UI.DpiUtil exists from keepass >= 2.27
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static int ScaleIntY(int i)
		{
			int result=-1;
			
			try{
				
				if (METHOD_ScaleIntX != null) {
					result = (int)METHOD_ScaleIntY.Invoke(null, new Object[]{i});
				}
				else {
					result = i;
				}
			}
			catch (Exception e) {
				Debug.Assert(false, "Error with ScaleIntY: " + e.ToString());
				result= -1;
			}
		    return result;
		}
		
		/// <summary>
		/// Get original image (to get Original Width for example)
		/// </summary>
		public static Image GetOriginalImage(PwCustomIcon icon)
		{
			Image result = null;
			EnsureInitialize();
			
			if (METHOD_GetImage_NoParameters != null) {
				result = METHOD_GetImage_NoParameters.Invoke(
					icon,
					new Object[] { } )
					as Image;
			}
			else {
				result = icon.GetType().GetProperty("Image").GetValue(
						icon, null) as Image;
			}
			
			return result;
				
		}
		
		/// <summary>
		/// Scale Image to fit dimensions (width x height). <br/>
		/// If version of keepass >= 2.28, use KeePass.UI.ScaleIntX/Y methods
		/// If version of keepass >= 2.29, use GetImage to get HighDPI definition
		/// </summary>
		public static Image GetScaledImage(PwCustomIcon icon, int width, int height)
		{
			Image result = null;
			EnsureInitialize();
			
			if (METHOD_GetImage_TwoParameters != null) {
				result = METHOD_GetImage_TwoParameters.Invoke(
					icon,
					new Object[] { ScaleIntX(width), ScaleIntY(height) } )
					as Image;
			}
			else {
				result = ResizedImage(
					icon.GetType().GetProperty("Image").GetValue(
						icon, null) as Image,
					ScaleIntX(width), ScaleIntY(height));
			}
			
			return result;
				
		}
		
		/// <summary>
		/// Get list of standards icons in high definition (if version is >= 2.29)
		/// </summary>
		/// <returns></returns>
		public static List<Image> GetHighDefinitionStandardIcons(
			KeePass.Plugins.IPluginHost iph, int width, int height) {
			List<Image> result = null;
			if (TYPE_ImageArchive != null ) {
				// cf MainForm_Functions.cs line 3234 from KeePass v2.31
				object myArchive = Activator.CreateInstance(TYPE_ImageArchive);
				   // type of myArchive should be ImageArchive
				METHOD_ImageArchive_Load.Invoke(
					myArchive,
					new object[] {PROPERTYVALUE_Images_Client_HighRes});
				result = (List<Image>)METHOD_ImageArchive_GetImages.Invoke(
				   	myArchive,
				   	new Object[]{width,height,true});
			}
			else {
				System.Windows.Forms.ImageList imgl = iph.MainWindow.ClientIcons;
				ImageList.ImageCollection ic = imgl.Images;
				result = new List<Image>();
				for (int i = 0; i<ic.Count; i++ ) {
					Image img = ResizedImage(ic[i],width,height);
					result.Add( img );
				}
				return result;
			}
			return result;
		}
		
		
		/// <summary>
		/// ResizedImage without DPIScaled utility
		/// </summary>
		public static Image ResizedImage(Image imgToBeConverted, int nWidth, int nHeight) {
			
				Image imgNew = imgToBeConverted;
				if(imgToBeConverted == null) { Debug.Assert(false); }

				if((imgToBeConverted.Width != nWidth) || (imgToBeConverted.Height != nHeight)) {
					
					if ( METHOD_ScaleImage != null ) {
						imgNew = METHOD_ScaleImage.Invoke(
							null,
							new Object[] { imgToBeConverted, nWidth, nHeight } )
							as Image;	
					}
					else {
						imgNew = new Bitmap(imgToBeConverted, new Size(nWidth, nHeight));
					}
				}
				
				return imgNew;
		}
		
	}
}
