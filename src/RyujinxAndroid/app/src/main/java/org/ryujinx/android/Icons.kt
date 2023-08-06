package org.ryujinx.android

import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.PathFillType
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.graphics.vector.path
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import compose.icons.CssGgIcons
import compose.icons.cssggicons.Games

class Icons {
    companion object{
        /// Icons exported from https://www.composables.com/icons
        @Composable
        fun download(): ImageVector {
            val primaryColor = MaterialTheme.colorScheme.primary
            return remember {
                ImageVector.Builder(
                    name = "download",
                    defaultWidth = 40.0.dp,
                    defaultHeight = 40.0.dp,
                    viewportWidth = 40.0f,
                    viewportHeight = 40.0f
                ).apply {
                    path(
                        fill = SolidColor(Color.Black.copy(alpha = 0.5f)),
                        stroke = SolidColor(primaryColor),
                        fillAlpha = 1f,
                        strokeAlpha = 1f,
                        strokeLineWidth = 1.0f,
                        strokeLineCap = StrokeCap.Butt,
                        strokeLineJoin = StrokeJoin.Miter,
                        strokeLineMiter = 1f,
                        pathFillType = PathFillType.NonZero
                    ) {
                        moveTo(20f, 26.25f)
                        quadToRelative(-0.25f, 0f, -0.479f, -0.083f)
                        quadToRelative(-0.229f, -0.084f, -0.438f, -0.292f)
                        lineToRelative(-6.041f, -6.083f)
                        quadToRelative(-0.417f, -0.375f, -0.396f, -0.917f)
                        quadToRelative(0.021f, -0.542f, 0.396f, -0.917f)
                        reflectiveQuadToRelative(0.916f, -0.396f)
                        quadToRelative(0.542f, -0.02f, 0.959f, 0.396f)
                        lineToRelative(3.791f, 3.792f)
                        verticalLineTo(8.292f)
                        quadToRelative(0f, -0.584f, 0.375f, -0.959f)
                        reflectiveQuadTo(20f, 6.958f)
                        quadToRelative(0.542f, 0f, 0.938f, 0.375f)
                        quadToRelative(0.395f, 0.375f, 0.395f, 0.959f)
                        verticalLineTo(21.75f)
                        lineToRelative(3.792f, -3.792f)
                        quadToRelative(0.375f, -0.416f, 0.917f, -0.396f)
                        quadToRelative(0.541f, 0.021f, 0.958f, 0.396f)
                        quadToRelative(0.375f, 0.375f, 0.375f, 0.917f)
                        reflectiveQuadToRelative(-0.375f, 0.958f)
                        lineToRelative(-6.083f, 6.042f)
                        quadToRelative(-0.209f, 0.208f, -0.438f, 0.292f)
                        quadToRelative(-0.229f, 0.083f, -0.479f, 0.083f)
                        close()
                        moveTo(9.542f, 32.958f)
                        quadToRelative(-1.042f, 0f, -1.834f, -0.791f)
                        quadToRelative(-0.791f, -0.792f, -0.791f, -1.834f)
                        verticalLineToRelative(-4.291f)
                        quadToRelative(0f, -0.542f, 0.395f, -0.938f)
                        quadToRelative(0.396f, -0.396f, 0.938f, -0.396f)
                        quadToRelative(0.542f, 0f, 0.917f, 0.396f)
                        reflectiveQuadToRelative(0.375f, 0.938f)
                        verticalLineToRelative(4.291f)
                        horizontalLineToRelative(20.916f)
                        verticalLineToRelative(-4.291f)
                        quadToRelative(0f, -0.542f, 0.375f, -0.938f)
                        quadToRelative(0.375f, -0.396f, 0.917f, -0.396f)
                        quadToRelative(0.583f, 0f, 0.958f, 0.396f)
                        reflectiveQuadToRelative(0.375f, 0.938f)
                        verticalLineToRelative(4.291f)
                        quadToRelative(0f, 1.042f, -0.791f, 1.834f)
                        quadToRelative(-0.792f, 0.791f, -1.834f, 0.791f)
                        close()
                    }
                }.build()
            }
        }
        @Composable
        fun vSync(): ImageVector {
            val primaryColor = MaterialTheme.colorScheme.primary
            return remember {
                ImageVector.Builder(
                    name = "60fps",
                    defaultWidth = 40.0.dp,
                    defaultHeight = 40.0.dp,
                    viewportWidth = 40.0f,
                    viewportHeight = 40.0f
                ).apply {
                    path(
                        fill = SolidColor(Color.Black.copy(alpha = 0.5f)),
                        stroke = SolidColor(primaryColor),
                        fillAlpha = 1f,
                        strokeAlpha = 1f,
                        strokeLineWidth = 1.0f,
                        strokeLineCap = StrokeCap.Butt,
                        strokeLineJoin = StrokeJoin.Miter,
                        strokeLineMiter = 1f,
                        pathFillType = PathFillType.NonZero
                    ) {
                        moveTo(7.292f, 31.458f)
                        quadToRelative(-1.542f, 0f, -2.625f, -1.041f)
                        quadToRelative(-1.084f, -1.042f, -1.084f, -2.625f)
                        verticalLineTo(12.208f)
                        quadToRelative(0f, -1.583f, 1.084f, -2.625f)
                        quadTo(5.75f, 8.542f, 7.292f, 8.542f)
                        horizontalLineTo(14f)
                        quadToRelative(0.75f, 0f, 1.292f, 0.541f)
                        quadToRelative(0.541f, 0.542f, 0.541f, 1.292f)
                        reflectiveQuadToRelative(-0.541f, 1.292f)
                        quadToRelative(-0.542f, 0.541f, -1.292f, 0.541f)
                        horizontalLineTo(7.208f)
                        verticalLineToRelative(5.084f)
                        horizontalLineToRelative(6.709f)
                        quadToRelative(1.541f, 0f, 2.583f, 1.041f)
                        quadToRelative(1.042f, 1.042f, 1.042f, 2.625f)
                        verticalLineToRelative(6.834f)
                        quadToRelative(0f, 1.583f, -1.042f, 2.625f)
                        quadToRelative(-1.042f, 1.041f, -2.583f, 1.041f)
                        close()
                        moveToRelative(-0.084f, -10.5f)
                        verticalLineToRelative(6.834f)
                        horizontalLineToRelative(6.709f)
                        verticalLineToRelative(-6.834f)
                        close()
                        moveToRelative(17.125f, 6.834f)
                        horizontalLineToRelative(8.459f)
                        verticalLineTo(12.208f)
                        horizontalLineToRelative(-8.459f)
                        verticalLineToRelative(15.584f)
                        close()
                        moveToRelative(0f, 3.666f)
                        quadToRelative(-1.541f, 0f, -2.583f, -1.041f)
                        quadToRelative(-1.042f, -1.042f, -1.042f, -2.625f)
                        verticalLineTo(12.208f)
                        quadToRelative(0f, -1.583f, 1.042f, -2.625f)
                        quadToRelative(1.042f, -1.041f, 2.583f, -1.041f)
                        horizontalLineToRelative(8.459f)
                        quadToRelative(1.541f, 0f, 2.583f, 1.041f)
                        quadToRelative(1.042f, 1.042f, 1.042f, 2.625f)
                        verticalLineToRelative(15.584f)
                        quadToRelative(0f, 1.583f, -1.042f, 2.625f)
                        quadToRelative(-1.042f, 1.041f, -2.583f, 1.041f)
                        close()
                    }
                }.build()
            }
        }
        @Composable
        fun videoGame(): ImageVector {
            val primaryColor = MaterialTheme.colorScheme.primary
            return remember {
                ImageVector.Builder(
                    name = "videogame_asset",
                    defaultWidth = 40.0.dp,
                    defaultHeight = 40.0.dp,
                    viewportWidth = 40.0f,
                    viewportHeight = 40.0f
                ).apply {
                    path(
                        fill = SolidColor(Color.Black.copy(alpha = 0.5f)),
                        stroke = SolidColor(primaryColor),
                        fillAlpha = 1f,
                        strokeAlpha = 1f,
                        strokeLineWidth = 1.0f,
                        strokeLineCap = StrokeCap.Butt,
                        strokeLineJoin = StrokeJoin.Miter,
                        strokeLineMiter = 1f,
                        pathFillType = PathFillType.NonZero
                    ) {
                        moveTo(6.25f, 29.792f)
                        quadToRelative(-1.083f, 0f, -1.854f, -0.792f)
                        quadToRelative(-0.771f, -0.792f, -0.771f, -1.833f)
                        verticalLineTo(12.833f)
                        quadToRelative(0f, -1.083f, 0.771f, -1.854f)
                        quadToRelative(0.771f, -0.771f, 1.854f, -0.771f)
                        horizontalLineToRelative(27.5f)
                        quadToRelative(1.083f, 0f, 1.854f, 0.771f)
                        quadToRelative(0.771f, 0.771f, 0.771f, 1.854f)
                        verticalLineToRelative(14.334f)
                        quadToRelative(0f, 1.041f, -0.771f, 1.833f)
                        reflectiveQuadToRelative(-1.854f, 0.792f)
                        close()
                        moveToRelative(0f, -2.625f)
                        horizontalLineToRelative(27.5f)
                        verticalLineTo(12.833f)
                        horizontalLineTo(6.25f)
                        verticalLineToRelative(14.334f)
                        close()
                        moveToRelative(7.167f, -1.792f)
                        quadToRelative(0.541f, 0f, 0.916f, -0.375f)
                        reflectiveQuadToRelative(0.375f, -0.917f)
                        verticalLineToRelative(-2.791f)
                        horizontalLineToRelative(2.75f)
                        quadToRelative(0.584f, 0f, 0.959f, -0.375f)
                        reflectiveQuadToRelative(0.375f, -0.917f)
                        quadToRelative(0f, -0.542f, -0.375f, -0.938f)
                        quadToRelative(-0.375f, -0.395f, -0.959f, -0.395f)
                        horizontalLineToRelative(-2.75f)
                        verticalLineToRelative(-2.75f)
                        quadToRelative(0f, -0.542f, -0.375f, -0.938f)
                        quadToRelative(-0.375f, -0.396f, -0.916f, -0.396f)
                        quadToRelative(-0.584f, 0f, -0.959f, 0.396f)
                        reflectiveQuadToRelative(-0.375f, 0.938f)
                        verticalLineToRelative(2.75f)
                        horizontalLineToRelative(-2.75f)
                        quadToRelative(-0.541f, 0f, -0.937f, 0.395f)
                        quadTo(8f, 19.458f, 8f, 20f)
                        quadToRelative(0f, 0.542f, 0.396f, 0.917f)
                        reflectiveQuadToRelative(0.937f, 0.375f)
                        horizontalLineToRelative(2.75f)
                        verticalLineToRelative(2.791f)
                        quadToRelative(0f, 0.542f, 0.396f, 0.917f)
                        reflectiveQuadToRelative(0.938f, 0.375f)
                        close()
                        moveToRelative(11.125f, -0.5f)
                        quadToRelative(0.791f, 0f, 1.396f, -0.583f)
                        quadToRelative(0.604f, -0.584f, 0.604f, -1.375f)
                        quadToRelative(0f, -0.834f, -0.604f, -1.417f)
                        quadToRelative(-0.605f, -0.583f, -1.396f, -0.583f)
                        quadToRelative(-0.834f, 0f, -1.417f, 0.583f)
                        quadToRelative(-0.583f, 0.583f, -0.583f, 1.375f)
                        quadToRelative(0f, 0.833f, 0.583f, 1.417f)
                        quadToRelative(0.583f, 0.583f, 1.417f, 0.583f)
                        close()
                        moveToRelative(3.916f, -5.833f)
                        quadToRelative(0.834f, 0f, 1.417f, -0.584f)
                        quadToRelative(0.583f, -0.583f, 0.583f, -1.416f)
                        quadToRelative(0f, -0.792f, -0.583f, -1.375f)
                        quadToRelative(-0.583f, -0.584f, -1.417f, -0.584f)
                        quadToRelative(-0.791f, 0f, -1.375f, 0.584f)
                        quadToRelative(-0.583f, 0.583f, -0.583f, 1.375f)
                        quadToRelative(0f, 0.833f, 0.583f, 1.416f)
                        quadToRelative(0.584f, 0.584f, 1.375f, 0.584f)
                        close()
                        moveTo(6.25f, 27.167f)
                        verticalLineTo(12.833f)
                        verticalLineToRelative(14.334f)
                        close()
                    }
                }.build()
            }
        }
    }
}

@Preview
@Composable
fun Preview(){
    IconButton(modifier = Modifier.padding(4.dp), onClick = {
    }) {
        Icon(
            imageVector = CssGgIcons.Games,
            contentDescription = "Open Panel"
        )
    }
}
