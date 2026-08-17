using System;
using UnityEngine;

namespace LumaLooks
{
	// Token: 0x02000045 RID: 69
	internal static class ShaderIds
	{
		// Token: 0x040004D8 RID: 1240
		public static readonly int BlitTexelSize = Shader.PropertyToID("_BlitTexture_TexelSize");

		// Token: 0x040004D9 RID: 1241
		public static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");

		// Token: 0x040004DA RID: 1242
		public static readonly int BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");

		// Token: 0x040004DB RID: 1243
		public static readonly int SunDirWS = Shader.PropertyToID("_LumaSunDirWS");

		// Token: 0x040004DC RID: 1244
		public static readonly int RayDirWS = Shader.PropertyToID("_LumaRayDirWS");

		// Token: 0x040004DD RID: 1245
		public static readonly int SunColor = Shader.PropertyToID("_LumaSunColor");

		// Token: 0x040004DE RID: 1246
		public static readonly int AmbientColor = Shader.PropertyToID("_LumaAmbientColor");

		// Token: 0x040004DF RID: 1247
		public static readonly int PrevVP0 = Shader.PropertyToID("_LumaPrevVP0");

		// Token: 0x040004E0 RID: 1248
		public static readonly int PrevVP1 = Shader.PropertyToID("_LumaPrevVP1");

		// Token: 0x040004E1 RID: 1249
		public static readonly int AOIntensity = Shader.PropertyToID("_LumaAOIntensity");

		// Token: 0x040004E2 RID: 1250
		public static readonly int AORadius = Shader.PropertyToID("_LumaAORadius");

		// Token: 0x040004E3 RID: 1251
		public static readonly int AOPower = Shader.PropertyToID("_LumaAOPower");

		// Token: 0x040004E4 RID: 1252
		public static readonly int AOSamples = Shader.PropertyToID("_LumaAOSamples");

		// Token: 0x040004E5 RID: 1253
		public static readonly int SSRIntensity = Shader.PropertyToID("_LumaSSRIntensity");

		// Token: 0x040004E6 RID: 1254
		public static readonly int SSRMaxDist = Shader.PropertyToID("_LumaSSRMaxDist");

		// Token: 0x040004E7 RID: 1255
		public static readonly int SSRSteps = Shader.PropertyToID("_LumaSSRSteps");

		// Token: 0x040004E8 RID: 1256
		public static readonly int SSRBlur = Shader.PropertyToID("_LumaSSRBlur");

		// Token: 0x040004E9 RID: 1257
		public static readonly int SSRSurfaceAware = Shader.PropertyToID("_LumaSSRSurfaceAware");

		// Token: 0x040004EA RID: 1258
		public static readonly int SSRMetalSharp = Shader.PropertyToID("_LumaSSRMetalSharp");

		// Token: 0x040004EB RID: 1259
		public static readonly int SSRSceneTex = Shader.PropertyToID("_LumaSSRSceneTex");

		// Token: 0x040004EC RID: 1260
		public static readonly int SunlightParams = Shader.PropertyToID("_LumaSunlightParams");

		// Token: 0x040004ED RID: 1261
		public static readonly int SunlightTint = Shader.PropertyToID("_LumaSunlightTint");

		// Token: 0x040004EE RID: 1262
		public static readonly int CloudShadow = Shader.PropertyToID("_LumaCloudShadow");

		// Token: 0x040004EF RID: 1263
		public static readonly int CloudTex = Shader.PropertyToID("_LumaCloudTex");

		// Token: 0x040004F0 RID: 1264
		public static readonly int CloudUpsampleOn = Shader.PropertyToID("_LumaCloudUpsampleParams");

		// Token: 0x040004F1 RID: 1265
		public static readonly int ShellRTSize = Shader.PropertyToID("_LumaShellRTSize");

		// Token: 0x040004F2 RID: 1266
		public static readonly int ShellRadius = Shader.PropertyToID("_LumaShellRadius");

		// Token: 0x040004F3 RID: 1267
		public static readonly int DepthPrimeTex = Shader.PropertyToID("_LumaDepthPrimeTex");

		// Token: 0x040004F4 RID: 1268
		public static readonly int DepthPrimeOn = Shader.PropertyToID("_LumaDepthPrimeOn");

		// Token: 0x040004F5 RID: 1269
		public static readonly int FxHalftoneScale = Shader.PropertyToID("_LumaFxHalftoneScale");

		// Token: 0x040004F6 RID: 1270
		public static readonly int FxHalftoneAmount = Shader.PropertyToID("_LumaFxHalftoneAmount");

		// Token: 0x040004F7 RID: 1271
		public static readonly int FxHalftoneColor = Shader.PropertyToID("_LumaFxHalftoneColor");

		// Token: 0x040004F8 RID: 1272
		public static readonly int FxCartoonSteps = Shader.PropertyToID("_LumaFxCartoonSteps");

		// Token: 0x040004F9 RID: 1273
		public static readonly int FxCartoonOutline = Shader.PropertyToID("_LumaFxCartoonOutline");

		// Token: 0x040004FA RID: 1274
		public static readonly int FxCartoonAmount = Shader.PropertyToID("_LumaFxCartoonAmount");

		// Token: 0x040004FB RID: 1275
		public static readonly int FxScanCount = Shader.PropertyToID("_LumaFxScanCount");

		// Token: 0x040004FC RID: 1276
		public static readonly int FxScanAmount = Shader.PropertyToID("_LumaFxScanAmount");

		// Token: 0x040004FD RID: 1277
		public static readonly int FxScanGrille = Shader.PropertyToID("_LumaFxScanGrille");

		// Token: 0x040004FE RID: 1278
		public static readonly int FxPixelSize = Shader.PropertyToID("_LumaFxPixelSize");

		// Token: 0x040004FF RID: 1279
		public static readonly int FxPixelAmount = Shader.PropertyToID("_LumaFxPixelAmount");

		// Token: 0x04000500 RID: 1280
		public static readonly int FxPixelLevels = Shader.PropertyToID("_LumaFxPixelLevels");

		// Token: 0x04000501 RID: 1281
		public static readonly int SunlightTex = Shader.PropertyToID("_LumaSunlightTex");

		// Token: 0x04000502 RID: 1282
		public static readonly int SunlightUpsample = Shader.PropertyToID("_LumaSunlightUpsample");

		// Token: 0x04000503 RID: 1283
		public static readonly int PlayerSun = Shader.PropertyToID("_LumaPlayerSun");

		// Token: 0x04000504 RID: 1284
		public static readonly int RigSun = Shader.PropertyToID("_LumaRigSun");

		// Token: 0x04000505 RID: 1285
		public static readonly int SunlightParams2 = Shader.PropertyToID("_LumaSunlightParams2");

		// Token: 0x04000506 RID: 1286
		public static readonly int SunlightParams3 = Shader.PropertyToID("_LumaSunlightParams3");

		// Token: 0x04000507 RID: 1287
		public static readonly int SkySunDir = Shader.PropertyToID("_LumaSkySunDir");

		// Token: 0x04000508 RID: 1288
		public static readonly int SkyParams = Shader.PropertyToID("_LumaSkyParams");

		// Token: 0x04000509 RID: 1289
		public static readonly int SkyParams2 = Shader.PropertyToID("_LumaSkyParams2");

		// Token: 0x0400050A RID: 1290
		public static readonly int SkyParams3 = Shader.PropertyToID("_LumaSkyParams3");

		// Token: 0x0400050B RID: 1291
		public static readonly int SkyDayZenith = Shader.PropertyToID("_LumaSkyDayZenith");

		// Token: 0x0400050C RID: 1292
		public static readonly int SkyDayHorizon = Shader.PropertyToID("_LumaSkyDayHorizon");

		// Token: 0x0400050D RID: 1293
		public static readonly int SkyDaySat = Shader.PropertyToID("_LumaSkyDaySat");

		// Token: 0x0400050E RID: 1294
		public static readonly int SkyDayHue = Shader.PropertyToID("_LumaSkyDayHue");

		// Token: 0x0400050F RID: 1295
		public static readonly int SkyAuroraA = Shader.PropertyToID("_LumaSkyAuroraA");

		// Token: 0x04000510 RID: 1296
		public static readonly int SkyAuroraB = Shader.PropertyToID("_LumaSkyAuroraB");

		// Token: 0x04000511 RID: 1297
		public static readonly int SkyBodyParams = Shader.PropertyToID("_LumaSkyBodyParams");

		// Token: 0x04000512 RID: 1298
		public static readonly int SkySunTint = Shader.PropertyToID("_LumaSkySunTint");

		// Token: 0x04000513 RID: 1299
		public static readonly int SkyMoonTint = Shader.PropertyToID("_LumaSkyMoonTint");

		// Token: 0x04000514 RID: 1300
		public static readonly int ShellDrawBody = Shader.PropertyToID("_LumaShellDrawBody");

		// Token: 0x04000515 RID: 1301
		public static readonly int CloudParams = Shader.PropertyToID("_LumaCloudParams");

		// Token: 0x04000516 RID: 1302
		public static readonly int CloudParams2 = Shader.PropertyToID("_LumaCloudParams2");

		// Token: 0x04000517 RID: 1303
		public static readonly int CloudParams3 = Shader.PropertyToID("_LumaCloudParams3");

		// Token: 0x04000518 RID: 1304
		public static readonly int CloudCamPos = Shader.PropertyToID("_LumaCloudCamPos");

		// Token: 0x04000519 RID: 1305
		public static readonly int MoonTex = Shader.PropertyToID("_LumaMoonTex");

		// Token: 0x0400051A RID: 1306
		public static readonly int CloudTint = Shader.PropertyToID("_LumaCloudTint");

		// Token: 0x0400051B RID: 1307
		public static readonly int SkyReplaceParams = Shader.PropertyToID("_LumaSkyReplaceParams");

		// Token: 0x0400051C RID: 1308
		public static readonly int SkyReplaceParams2 = Shader.PropertyToID("_LumaSkyReplaceParams2");

		// Token: 0x0400051D RID: 1309
		public static readonly int GameTintColor = Shader.PropertyToID("_TintColor");

		// Token: 0x0400051E RID: 1310
		public static readonly int GameColor = Shader.PropertyToID("_Color");

		// Token: 0x0400051F RID: 1311
		public static readonly int GameBaseColor = Shader.PropertyToID("_BaseColor");

		// Token: 0x04000520 RID: 1312
		public static readonly int GIIntensity = Shader.PropertyToID("_LumaGIIntensity");

		// Token: 0x04000521 RID: 1313
		public static readonly int GIRadius = Shader.PropertyToID("_LumaGIRadius");

		// Token: 0x04000522 RID: 1314
		public static readonly int GIRays = Shader.PropertyToID("_LumaGIRays");

		// Token: 0x04000523 RID: 1315
		public static readonly int GIColorBleed = Shader.PropertyToID("_LumaGIColorBleed");

		// Token: 0x04000524 RID: 1316
		public static readonly int FrameIndex = Shader.PropertyToID("_LumaFrameIndex");

		// Token: 0x04000525 RID: 1317
		public static readonly int GIPrevTex = Shader.PropertyToID("_LumaGIPrevTex");

		// Token: 0x04000526 RID: 1318
		public static readonly int GISharpness = Shader.PropertyToID("_LumaGISharpness");

		// Token: 0x04000527 RID: 1319
		public static readonly int GIDenoise = Shader.PropertyToID("_LumaGIDenoise");

		// Token: 0x04000528 RID: 1320
		public static readonly int GITemporal = Shader.PropertyToID("_LumaGITemporal");

		// Token: 0x04000529 RID: 1321
		public static readonly int PShadowIntensity = Shader.PropertyToID("_LumaPlayerShadowIntensity");

		// Token: 0x0400052A RID: 1322
		public static readonly int PShadowSoftness = Shader.PropertyToID("_LumaPlayerShadowSoftness");

		// Token: 0x0400052B RID: 1323
		public static readonly int PShadowLight = Shader.PropertyToID("_LumaPlayerShadowLight");

		// Token: 0x0400052C RID: 1324
		public static readonly int MaskTex = Shader.PropertyToID("_LumaMaskTex");

		// Token: 0x0400052D RID: 1325
		public static readonly int MaskValid = Shader.PropertyToID("_LumaMaskValid");

		// Token: 0x0400052E RID: 1326
		public static readonly int MaskTexel = Shader.PropertyToID("_LumaMaskTexel");

		// Token: 0x0400052F RID: 1327
		public static readonly int RayDebug = Shader.PropertyToID("_LumaRayDebug");

		// Token: 0x04000530 RID: 1328
		public static readonly int MaskTier = Shader.PropertyToID("_LumaMaskTier");

		// Token: 0x04000531 RID: 1329
		public static readonly int TDReach = Shader.PropertyToID("_LumaTDReach");

		// Token: 0x04000532 RID: 1330
		public static readonly int TDIntensity = Shader.PropertyToID("_LumaTDIntensity");

		// Token: 0x04000533 RID: 1331
		public static readonly int TDFloor = Shader.PropertyToID("_LumaTDFloor");

		// Token: 0x04000534 RID: 1332
		public static readonly int TDEnclosure = Shader.PropertyToID("_LumaTDEnclosure");

		// Token: 0x04000535 RID: 1333
		public static readonly int PlayerShade = Shader.PropertyToID("_LumaPlayerShade");

		// Token: 0x04000536 RID: 1334
		public static readonly int RayExtinctionRelief = Shader.PropertyToID("_LumaRayExtinctionRelief");

		// Token: 0x04000537 RID: 1335
		public static readonly int TDTex = Shader.PropertyToID("_LumaTDTex");

		// Token: 0x04000538 RID: 1336
		public static readonly int AOTex = Shader.PropertyToID("_LumaAOTex");

		// Token: 0x04000539 RID: 1337
		public static readonly int SSRTex = Shader.PropertyToID("_LumaSSRTex");

		// Token: 0x0400053A RID: 1338
		public static readonly int GITex = Shader.PropertyToID("_LumaGITex");

		// Token: 0x0400053B RID: 1339
		public static readonly int CompositeFlags = Shader.PropertyToID("_LumaCompositeFlags");

		// Token: 0x0400053C RID: 1340
		public static readonly int CompositeFlags2 = Shader.PropertyToID("_LumaCompositeFlags2");

		// Token: 0x0400053D RID: 1341
		public static readonly int HazeDensity = Shader.PropertyToID("_LumaHazeDensity");

		// Token: 0x0400053E RID: 1342
		public static readonly int HazeStart = Shader.PropertyToID("_LumaHazeStart");

		// Token: 0x0400053F RID: 1343
		public static readonly int HazeSunScatter = Shader.PropertyToID("_LumaHazeSunScatter");

		// Token: 0x04000540 RID: 1344
		public static readonly int HazeHeightFalloff = Shader.PropertyToID("_LumaHazeHeightFalloff");

		// Token: 0x04000541 RID: 1345
		public static readonly int HazeWisps = Shader.PropertyToID("_LumaHazeWisps");

		// Token: 0x04000542 RID: 1346
		public static readonly int HazeSkyVeil = Shader.PropertyToID("_LumaHazeSkyVeil");

		// Token: 0x04000543 RID: 1347
		public static readonly int HazeMaxBrightness = Shader.PropertyToID("_LumaHazeMaxBrightness");

		// Token: 0x04000544 RID: 1348
		public static readonly int HazeTint = Shader.PropertyToID("_LumaHazeTint");

		// Token: 0x04000545 RID: 1349
		public static readonly int WetStrength = Shader.PropertyToID("_LumaWetStrength");

		// Token: 0x04000546 RID: 1350
		public static readonly int HighlightBoost = Shader.PropertyToID("_LumaHighlightBoost");

		// Token: 0x04000547 RID: 1351
		public static readonly int GIUpsample = Shader.PropertyToID("_LumaGIUpsample");

		// Token: 0x04000548 RID: 1352
		public static readonly int AOUpsample = Shader.PropertyToID("_LumaAOUpsample");

		// Token: 0x04000549 RID: 1353
		public static readonly int GIEmissive = Shader.PropertyToID("_LumaGIEmissive");

		// Token: 0x0400054A RID: 1354
		public static readonly int FlareIntensity = Shader.PropertyToID("_LumaFlareIntensity");

		// Token: 0x0400054B RID: 1355
		public static readonly int FlareStreakLen = Shader.PropertyToID("_LumaFlareStreakLen");

		// Token: 0x0400054C RID: 1356
		public static readonly int FlareMode = Shader.PropertyToID("_LumaFlareMode");

		// Token: 0x0400054D RID: 1357
		public static readonly int FlareParams = Shader.PropertyToID("_LumaFlareParams");

		// Token: 0x0400054E RID: 1358
		public static readonly int RainVisibility = Shader.PropertyToID("_LumaRainVisibility");

		// Token: 0x0400054F RID: 1359
		public static readonly int CoverageValid = Shader.PropertyToID("_LumaCoverageValid");

		// Token: 0x04000550 RID: 1360
		public static readonly int SSRCoverageValid = Shader.PropertyToID("_LumaSSRCoverageValid");

		// Token: 0x04000551 RID: 1361
		public static readonly int SSRRainOnly = Shader.PropertyToID("_LumaSSRRainOnly");

		// Token: 0x04000552 RID: 1362
		public static readonly int RainOpacity = Shader.PropertyToID("_LumaRainOpacity");

		// Token: 0x04000553 RID: 1363
		public static readonly int RainMode = Shader.PropertyToID("_LumaRainMode");

		// Token: 0x04000554 RID: 1364
		public static readonly int WindGlobal = Shader.PropertyToID("_LumaWindWS_G");

		// Token: 0x04000555 RID: 1365
		public static readonly int StormFactorGlobal = Shader.PropertyToID("_LumaStormFactor");

		// Token: 0x04000556 RID: 1366
		public static readonly int ParticleKind = Shader.PropertyToID("_LumaParticleKind");

		// Token: 0x04000557 RID: 1367
		public static readonly int ParticleBrightness = Shader.PropertyToID("_LumaParticleBrightness");

		// Token: 0x04000558 RID: 1368
		public static readonly int ParticleGlow = Shader.PropertyToID("_LumaParticleGlow");

		// Token: 0x04000559 RID: 1369
		public static readonly int ParticleShape = Shader.PropertyToID("_LumaParticleShape");

		// Token: 0x0400055A RID: 1370
		public static readonly int ParticleLeafType = Shader.PropertyToID("_LumaParticleLeafType");

		// Token: 0x0400055B RID: 1371
		public static readonly int LeafTex = Shader.PropertyToID("_LumaLeafTex");

		// Token: 0x0400055C RID: 1372
		public static readonly int LeafSlice = Shader.PropertyToID("_LumaLeafSlice");

		// Token: 0x0400055D RID: 1373
		public static readonly int LeafHasTex = Shader.PropertyToID("_LumaLeafHasTex");

		// Token: 0x0400055E RID: 1374
		public static readonly int GtBaseMapAtlas = Shader.PropertyToID("_BaseMap_Atlas");

		// Token: 0x0400055F RID: 1375
		public static readonly int GtBaseMapSlice = Shader.PropertyToID("_BaseMap_AtlasSlice");

		// Token: 0x04000560 RID: 1376
		public static readonly int RainFactorGlobal = Shader.PropertyToID("_LumaRainFactor");

		// Token: 0x04000561 RID: 1377
		public static readonly int WetBuildupGlobal = Shader.PropertyToID("_LumaWetBuildup");

		// Token: 0x04000562 RID: 1378
		public static readonly int SourceIsMoonGlobal = Shader.PropertyToID("_LumaSourceIsMoon");

		// Token: 0x04000563 RID: 1379
		public static readonly int CamCoveredGlobal = Shader.PropertyToID("_LumaCamCovered");

		// Token: 0x04000564 RID: 1380
		public static readonly int BloomThreshold = Shader.PropertyToID("_LumaBloomThreshold");

		// Token: 0x04000565 RID: 1381
		public static readonly int BloomScatter = Shader.PropertyToID("_LumaBloomScatter");

		// Token: 0x04000566 RID: 1382
		public static readonly int BloomLowTex = Shader.PropertyToID("_LumaBloomLowTex");

		// Token: 0x04000567 RID: 1383
		public static readonly int BloomTex = Shader.PropertyToID("_LumaBloomTex");

		// Token: 0x04000568 RID: 1384
		public static readonly int BloomIntensity = Shader.PropertyToID("_LumaBloomIntensity");

		// Token: 0x04000569 RID: 1385
		public static readonly int BloomTint = Shader.PropertyToID("_LumaBloomTint");

		// Token: 0x0400056A RID: 1386
		public static readonly int BloomHighlights = Shader.PropertyToID("_LumaBloomHighlights");

		// Token: 0x0400056B RID: 1387
		public static readonly int DoFFocusDist = Shader.PropertyToID("_LumaDoFFocusDist");

		// Token: 0x0400056C RID: 1388
		public static readonly int DoFStrength = Shader.PropertyToID("_LumaDoFStrength");

		// Token: 0x0400056D RID: 1389
		public static readonly int DoFMaxRadius = Shader.PropertyToID("_LumaDoFMaxRadius");

		// Token: 0x0400056E RID: 1390
		public static readonly int DoFAutoFocus = Shader.PropertyToID("_LumaDoFAutoFocus");

		// Token: 0x0400056F RID: 1391
		public static readonly int DoFNearStrength = Shader.PropertyToID("_LumaDoFNearStrength");

		// Token: 0x04000570 RID: 1392
		public static readonly int DoFBokehGamma = Shader.PropertyToID("_LumaDoFBokehGamma");

		// Token: 0x04000571 RID: 1393
		public static readonly int DoFHalfResTex = Shader.PropertyToID("_LumaDoFHalfResTex");

		// Token: 0x04000572 RID: 1394
		public static readonly int BlurMode = Shader.PropertyToID("_LumaBlurMode");

		// Token: 0x04000573 RID: 1395
		public static readonly int DistBlurStart = Shader.PropertyToID("_LumaDistBlurStart");

		// Token: 0x04000574 RID: 1396
		public static readonly int DistBlurEnd = Shader.PropertyToID("_LumaDistBlurEnd");

		// Token: 0x04000575 RID: 1397
		public static readonly int DoFFocusTex = Shader.PropertyToID("_LumaDoFFocusTex");

		// Token: 0x04000576 RID: 1398
		public static readonly int DoFFocusPrev = Shader.PropertyToID("_LumaDoFFocusPrev");

		// Token: 0x04000577 RID: 1399
		public static readonly int DoFFocusSpeed = Shader.PropertyToID("_LumaDoFFocusSpeed");

		// Token: 0x04000578 RID: 1400
		public static readonly int DeltaTime = Shader.PropertyToID("_LumaDeltaTime");

		// Token: 0x04000579 RID: 1401
		public static readonly int FlareVisTex = Shader.PropertyToID("_LumaFlareVisTex");

		// Token: 0x0400057A RID: 1402
		public static readonly int FlareEaseRate = Shader.PropertyToID("_LumaFlareEaseRate");

		// Token: 0x0400057B RID: 1403
		public static readonly int FlareEaseValid = Shader.PropertyToID("_LumaFlareEaseValid");

		// Token: 0x0400057C RID: 1404
		public static readonly int MBAmount = Shader.PropertyToID("_LumaMBAmount");

		// Token: 0x0400057D RID: 1405
		public static readonly int MBSamples = Shader.PropertyToID("_LumaMBSamples");

		// Token: 0x0400057E RID: 1406
		public static readonly int Exposure = Shader.PropertyToID("_LumaExposure");

		// Token: 0x0400057F RID: 1407
		public static readonly int WBWarmth = Shader.PropertyToID("_LumaWBWarmth");

		// Token: 0x04000580 RID: 1408
		public static readonly int WBTint = Shader.PropertyToID("_LumaWBTint");

		// Token: 0x04000581 RID: 1409
		public static readonly int Contrast = Shader.PropertyToID("_LumaContrast");

		// Token: 0x04000582 RID: 1410
		public static readonly int Whites = Shader.PropertyToID("_LumaWhites");

		// Token: 0x04000583 RID: 1411
		public static readonly int Blacks = Shader.PropertyToID("_LumaBlacks");

		// Token: 0x04000584 RID: 1412
		public static readonly int Saturation = Shader.PropertyToID("_LumaSaturation");

		// Token: 0x04000585 RID: 1413
		public static readonly int Vibrance = Shader.PropertyToID("_LumaVibrance");

		// Token: 0x04000586 RID: 1414
		public static readonly int FilmLook = Shader.PropertyToID("_LumaFilmLook");

		// Token: 0x04000587 RID: 1415
		public static readonly int FilmStrength = Shader.PropertyToID("_LumaFilmStrength");

		// Token: 0x04000588 RID: 1416
		public static readonly int Drama = Shader.PropertyToID("_LumaDrama");

		// Token: 0x04000589 RID: 1417
		public static readonly int Tonemap = Shader.PropertyToID("_LumaTonemap");

		// Token: 0x0400058A RID: 1418
		public static readonly int Vignette = Shader.PropertyToID("_LumaVignette");

		// Token: 0x0400058B RID: 1419
		public static readonly int PuddlesGlobal = Shader.PropertyToID("_LumaPuddles");

		// Token: 0x0400058C RID: 1420
		public static readonly int Puddles2Global = Shader.PropertyToID("_LumaPuddles2");

		// Token: 0x0400058D RID: 1421
		public static readonly int Puddles3Global = Shader.PropertyToID("_LumaPuddles3");

		// Token: 0x0400058E RID: 1422
		public static readonly int Grain = Shader.PropertyToID("_LumaGrain");

		// Token: 0x0400058F RID: 1423
		public static readonly int GrainSpeed = Shader.PropertyToID("_LumaGrainSpeed");

		// Token: 0x04000590 RID: 1424
		public static readonly int Chromatic = Shader.PropertyToID("_LumaChromatic");

		// Token: 0x04000591 RID: 1425
		public static readonly int Deband = Shader.PropertyToID("_LumaDeband");

		// Token: 0x04000592 RID: 1426
		public static readonly int Letterbox = Shader.PropertyToID("_LumaLetterbox");

		// Token: 0x04000593 RID: 1427
		public static readonly int UberFlags = Shader.PropertyToID("_LumaUberFlags");

		// Token: 0x04000594 RID: 1428
		public static readonly int FXAAQuality = Shader.PropertyToID("_LumaFXAAQuality");

		// Token: 0x04000595 RID: 1429
		public static readonly int TextMaskTex = Shader.PropertyToID("_LumaTextMask");

		// Token: 0x04000596 RID: 1430
		public static readonly int TextMaskValid = Shader.PropertyToID("_LumaTextMaskValid");

		// Token: 0x04000597 RID: 1431
		public static readonly int CASAmount = Shader.PropertyToID("_LumaCASAmount");

		// Token: 0x04000598 RID: 1432
		public static readonly int VrBalanced = Shader.PropertyToID("_LumaVrBalanced");

		// Token: 0x04000599 RID: 1433
		public static readonly int SunDirWSGlobal = Shader.PropertyToID("_LumaSunDirWS_G");

		// Token: 0x0400059A RID: 1434
		public static readonly int SunColorGlobal = Shader.PropertyToID("_LumaSunColor_G");

		// Token: 0x0400059B RID: 1435
		public static readonly int SceneFogParamsGlobal = Shader.PropertyToID("_LumaSceneFogParams_G");

		// Token: 0x0400059C RID: 1436
		public static readonly int SceneFogColorGlobal = Shader.PropertyToID("_LumaSceneFogColor_G");

		// Token: 0x0400059D RID: 1437
		public static readonly int WaterPlaneGlobal = Shader.PropertyToID("_LumaWaterPlane_G");

		// Token: 0x0400059E RID: 1438
		public static readonly int WaterPlane2Global = Shader.PropertyToID("_LumaWaterPlane2_G");

		// Token: 0x0400059F RID: 1439
		public static readonly int WaterHeaveAmpGlobal = Shader.PropertyToID("_LumaWaterHeaveAmp_G");

		// Token: 0x040005A0 RID: 1440
		public static readonly int HazeParamsGlobal = Shader.PropertyToID("_LumaHazeParams_G");

		// Token: 0x040005A1 RID: 1441
		public static readonly int HazeParams2Global = Shader.PropertyToID("_LumaHazeParams2_G");

		// Token: 0x040005A2 RID: 1442
		public static readonly int AmbientColorGlobal = Shader.PropertyToID("_LumaAmbientColor_G");

		// Token: 0x040005A3 RID: 1443
		public static readonly int WaterWaveStrength = Shader.PropertyToID("_LumaWaterWaveStrength");

		// Token: 0x040005A4 RID: 1444
		public static readonly int WaterWaveSpeed = Shader.PropertyToID("_LumaWaterWaveSpeed");

		// Token: 0x040005A5 RID: 1445
		public static readonly int WaterWaveHeight = Shader.PropertyToID("_LumaWaterWaveHeight");

		// Token: 0x040005A6 RID: 1446
		public static readonly int WaterClarity = Shader.PropertyToID("_LumaWaterClarity");

		// Token: 0x040005A7 RID: 1447
		public static readonly int WaterReflection = Shader.PropertyToID("_LumaWaterReflection");

		// Token: 0x040005A8 RID: 1448
		public static readonly int WaterSpecClamp = Shader.PropertyToID("_LumaWaterSpecClamp");

		// Token: 0x040005A9 RID: 1449
		public static readonly int WaterRefraction = Shader.PropertyToID("_LumaWaterRefraction");

		// Token: 0x040005AA RID: 1450
		public static readonly int WaterGlint = Shader.PropertyToID("_LumaWaterGlint");

		// Token: 0x040005AB RID: 1451
		public static readonly int WaterOpaqueValidGlobal = Shader.PropertyToID("_LumaWaterOpaqueValid");

		// Token: 0x040005AC RID: 1452
		public static readonly int WaterSpectrum = Shader.PropertyToID("_LumaWaterSpectrum");

		// Token: 0x040005AD RID: 1453
		public static readonly int WaterDeepTint = Shader.PropertyToID("_LumaWaterDeepTint");

		// Token: 0x040005AE RID: 1454
		public static readonly int WaterShallowTint = Shader.PropertyToID("_LumaWaterShallowTint");

		// Token: 0x040005AF RID: 1455
		public static readonly int WaterScatter = Shader.PropertyToID("_LumaWaterScatter");

		// Token: 0x040005B0 RID: 1456
		public static readonly int WaterSigma = Shader.PropertyToID("_LumaWaterSigma");

		// Token: 0x040005B1 RID: 1457
		public static readonly int WaterRough = Shader.PropertyToID("_LumaWaterRough");

		// Token: 0x040005B2 RID: 1458
		public static readonly int WaterSkyTint = Shader.PropertyToID("_LumaWaterSkyTint");

		// Token: 0x040005B3 RID: 1459
		public static readonly int WaterUseBaseTex = Shader.PropertyToID("_LumaWaterUseBaseTex");

		// Token: 0x040005B4 RID: 1460
		public static readonly int WaterScatterGlobal = Shader.PropertyToID("_LumaWaterScatter_G");

		// Token: 0x040005B5 RID: 1461
		public static readonly int WaterSigmaGlobal = Shader.PropertyToID("_LumaWaterSigma_G");

		// Token: 0x040005B6 RID: 1462
		public static readonly int WaterBaseTex = Shader.PropertyToID("_LumaWaterBaseTex");

		// Token: 0x040005B7 RID: 1463
		public static readonly int WaterBaseTexST = Shader.PropertyToID("_LumaWaterBaseTex_ST");

		// Token: 0x040005B8 RID: 1464
		public static readonly int WaterBaseColor = Shader.PropertyToID("_LumaWaterBaseColor");

		// Token: 0x040005B9 RID: 1465
		public static readonly int WaterBodyScale = Shader.PropertyToID("_LumaWaterBodyScale");

		// Token: 0x040005BA RID: 1466
		public static readonly int WaterHasBaseTex = Shader.PropertyToID("_LumaWaterHasBaseTex");

		// Token: 0x040005BB RID: 1467
		public static readonly int GameMainTex = Shader.PropertyToID("_MainTex");

		// Token: 0x040005BC RID: 1468
		public static readonly int GameMainTexST = Shader.PropertyToID("_MainTex_ST");

		// Token: 0x040005BD RID: 1469
		public static readonly int GameBaseMap = Shader.PropertyToID("_BaseMap");

		// Token: 0x040005BE RID: 1470
		public static readonly int GameBaseMapST = Shader.PropertyToID("_BaseMap_ST");

		// Token: 0x040005BF RID: 1471
		public static readonly int HazeTintGlobal = Shader.PropertyToID("_LumaHazeTint_G");

		// Token: 0x040005C0 RID: 1472
		public static readonly int WaterSSRTexGlobal = Shader.PropertyToID("_LumaWaterSSRTex_G");

		// Token: 0x040005C1 RID: 1473
		public static readonly int WaterSSRParamsGlobal = Shader.PropertyToID("_LumaWaterSSRParams_G");

		// Token: 0x040005C2 RID: 1474
		public static readonly int Underwater = Shader.PropertyToID("_LumaUnderwater");

		// Token: 0x040005C3 RID: 1475
		public static readonly int UWDistort = Shader.PropertyToID("_LumaUWDistort");

		// Token: 0x040005C4 RID: 1476
		public static readonly int UWBlur = Shader.PropertyToID("_LumaUWBlur");

		// Token: 0x040005C5 RID: 1477
		public static readonly int UWFogDensity = Shader.PropertyToID("_LumaUWFogDensity");

		// Token: 0x040005C6 RID: 1478
		public static readonly int UWCaustics = Shader.PropertyToID("_LumaUWCaustics");
	}
}
