/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
package grandroid.view;

import android.app.Activity;
import android.app.Dialog;
import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Color;
import android.graphics.Matrix;
import android.graphics.drawable.BitmapDrawable;
import android.util.DisplayMetrics;
import android.util.Log;
import android.view.Display;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.view.ViewGroup.LayoutParams;
import android.widget.AdapterView.OnItemClickListener;
import android.widget.AdapterView.OnItemLongClickListener;
import android.widget.*;
import java.io.InputStream;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;

/**
 *
 * @author Rovers
 */
public class LayoutMaker {

    /**
     *
     */
    protected ViewGroup lastLayout;
    /**
     *
     */
    protected LinearLayout mainLayout;
    /**
     *
     */
    protected int viewID = 1;
    /**
     *
     */
    protected ViewDesigner designer;
    /**
     *
     */
    protected Context context;
    /**
     *
     */
    protected Matrix m = new Matrix();
    //protected DisplayAgent da;
    protected boolean autoLoadResourceAsBitmap;
    protected boolean autoScaleResource;
    protected ViewGroup savedLayout;
    public final static int SIDEMENU_SHIFTMODE_RIGHT_FIT_BUTTON = 0;
    public final static int SIDEMENU_SHIFTMODE_RIGHT = 1;
    public final static int SIDEMENU_SHIFTMODE_LEFT = 2;

    /**
     *
     * @param context
     */
    public LayoutMaker(Activity context) {
        this.context = context;
        designer = new ViewDesigner();
        LinearLayout ll = new LinearLayout(context);
        designer.stylise(ll);
        ll.setOrientation(LinearLayout.VERTICAL);
        mainLayout = ll;
        lastLayout = ll;
        context.setContentView(mainLayout);
        //da = new DisplayAgent(context);
        updateMatrix(context, 640);
    }

    /**
     *
     * @param context
     * @param layout
     */
    public LayoutMaker(Context context, LinearLayout layout) {
        this(context, layout, false);
    }

    /**
     *
     * @param context
     * @param layout
     * @param createFakeRoot
     */
    public LayoutMaker(Context context, LinearLayout layout, boolean createFakeRoot) {
        this.context = context;
        designer = new ViewDesigner();
        if (layout.getParent() == null && createFakeRoot) {
            LinearLayout fakeRoot = new LinearLayout(context);
            fakeRoot.setOrientation(LinearLayout.VERTICAL);
            fakeRoot.addView(layout, new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.MATCH_PARENT));
            this.mainLayout = fakeRoot;
        } else {
            this.mainLayout = layout;
        }
        lastLayout = layout;
        if (context instanceof Activity) {
            updateMatrix((Activity) context, 640);
        }
    }

    /**
     *
     * @param dialog
     * @param context
     */
    public LayoutMaker(Dialog dialog, Context context) {
        this.context = context;
        LinearLayout ll = new LinearLayout(context);
        designer.stylise(ll);
        ll.setOrientation(LinearLayout.VERTICAL);
        mainLayout = ll;
        lastLayout = ll;
        dialog.setContentView(mainLayout);
        if (context instanceof Activity) {
            updateMatrix((Activity) context, 640);
        }
    }

    public void updateMatrix(Activity activity, int designWidth) {
        final DisplayMetrics metrics = new DisplayMetrics();
        Display display = activity.getWindowManager().getDefaultDisplay();
        Method mGetRawW = null;
        int realWidth = 0;

        try {
            display.getMetrics(metrics);
            realWidth = metrics.widthPixels;
        } catch (Exception ex) {

        }
        if (realWidth > 0) {
            float ratio = realWidth / (float) designWidth;
            m.setScale(ratio, ratio);
        }
    }

    public void setDrawableDesignWidth(Activity context, int width) {
        updateMatrix(context, width);
    }

    public Matrix getMatrix() {
        return m;
    }

    /**
     *
     * @param designer
     */
    public void setDesigner(ViewDesigner designer) {
        this.designer = designer;
        if (designer != null) {
            designer.stylise(mainLayout);
        }
    }

    /**
     *
     * @return
     */
    public ViewDesigner getDesigner() {
        return designer;
    }

    public void setLastLayout(ViewGroup lastLayout) {
        this.lastLayout = lastLayout;
    }

    /**
     *
     * @return
     */
    public ViewGroup getLastLayout() {
        return lastLayout;
    }

    /**
     *
     * @return
     */
    public LinearLayout getMainLayout() {
        return mainLayout;
    }

    /**
     *
     * @return
     */
    public ViewGroup getRootLayout() {
        if (mainLayout.getParent() instanceof ScrollView || mainLayout.getParent() instanceof HorizontalScrollView) {
            return (ViewGroup) mainLayout.getParent().getParent();
        } else if (mainLayout.getParent() instanceof LinearLayout || mainLayout.getParent() instanceof RelativeLayout) {
            return (ViewGroup) mainLayout.getParent();
        } else {
            return mainLayout;
        }
    }

    /**
     * 新增一個view元件至"目前Layout" 加入Layout時的參數為layWW(0)
     *
     * @param <T>
     * @param view 任何view物件，如某種Layout、TextView、EditText、ListView或ImageView等等
     * @return view本身
     */
    public <T extends View> T add(T view) {
        lastLayout.addView(view);
        return view;
    }

    /**
     * 新增一個view元件至"目前Layout"，依傳入的LinearLayout.LayoutParams物件
     *
     * @param <T>
     * @param view 任何view物件，如某種Layout、TextView、EditText、ListView或ImageView等等
     * @param params 一般不需自己生成，而是使用layFF()、layFW()、layWW(0)、layWW(1)代替
     * @return view本身
     */
    public <T extends View> T add(T view, ViewGroup.LayoutParams params) {
        lastLayout.addView(view, params);
        return view;
    }

    /**
     * 產生一個文字標籤
     *
     * @param text 將顯示的文字
     * @return 生成的物件
     */
    public TextView createTextView(CharSequence text) {
        TextView tv = new TextView(context);
        tv.setText(text);
        designer.stylise(tv);
        return tv;
    }

    public StyledText createStyledText(CharSequence text) {
        TextView tv = new TextView(context);
        tv.setText(text);
        return new StyledText().setMatrix(m).set(designer.stylise(tv));
    }

    public StyledText createStyledEdit(CharSequence text) {
        EditText et = new EditText(context);
        et.setBackgroundColor(Color.TRANSPARENT);
        et.setPadding(0, 0, 0, 0);
        et.setText(text);
        return new StyledText().setMatrix(m).set(designer.stylise(et));
    }

    /**
     * 產生一個文字標籤，並加入到"目前Layout" 加入Layout時的參數為layWW(0)
     *
     * @param text 將顯示的文字
     * @return 生成的物件
     */
    public TextView addTextView(CharSequence text) {
        TextView tv = createTextView(text);
        lastLayout.addView(tv);
        return tv;
    }

    /**
     * 產生一個文字方塊
     *
     * @param text 將顯示的文字
     * @return 生成的物件
     */
    public EditText createEditText(CharSequence text) {
        EditText et = new EditText(context);
        designer.stylise(et);
        et.setText(text);
        return et;
    }

    /**
     * 產生一個文字方塊，並加入到"目前Layout" 加入Layout時的參數為layWW(0)
     *
     * @param text 將顯示的文字
     * @return 生成的物件
     */
    public EditText addEditText(CharSequence text) {
        return addEditText(text, false);
    }

    /**
     * 產生一個文字方塊，並加入到"目前Layout" 加入Layout時的參數為layWW(0)
     *
     * @param text 將顯示的文字
     * @param fillParent layout參數width是否為fillparent
     * @return 生成的物件
     */
    public EditText addEditText(CharSequence text, boolean fillParent) {
        EditText et = createEditText(text);
        if (fillParent) {
            lastLayout.addView(et, layFW());
        } else {
            lastLayout.addView(et);
        }
        return et;
    }

    /**
     * 產生一個按鈕
     *
     * @param text 將顯示的文字
     * @return 生成的物件
     */
    public Button createButton(CharSequence text) {
        Button btn = new Button(context);
        designer.stylise(btn);
        btn.setText(text);
        return btn;
    }

    /**
     * 產生一個按鈕，並加入到"目前Layout"
     * 加入Layout時的參數：當"目前Layout"的Orientation橫向時為layWW(1)；其他狀況為layWW(0)
     *
     * @param text 將顯示的文字
     * @return 生成的物件
     */
    public Button addButton(CharSequence text) {
        Button btn = createButton(text);
        lastLayout.addView(btn, layWW(lastLayout instanceof LinearLayout && ((LinearLayout) lastLayout).getOrientation() == LinearLayout.HORIZONTAL ? 1 : 0));
        return btn;
    }

    /**
     * 產生一個影像按鈕
     *
     * @param <T> 需要產生影像物件的類別，一般為ImageView.class或ImageButton.class
     * @param viewClass 需要產生影像物件的類別，一般為ImageView.class或ImageButton.class
     * @param resourceID Resource ID
     * @return 生成的物件
     */
    public <T extends ImageView> T createImage(Class<T> viewClass, int resourceID) {
        try {
            T iv = viewClass.getConstructor(Context.class).newInstance(context);
            designer.stylise(iv);
            if (!autoLoadResourceAsBitmap || resourceID == 0) {
                iv.setImageResource(resourceID);
            } else {
                iv.setImageBitmap(loadResourceImage(resourceID));
            }
            return iv;
        } catch (Exception ex) {
            Log.e("grandroid-layout", null, ex);
            return null;
        }
    }

    /**
     * 使用網路上的圖片產生一個影像按鈕
     *
     * @param <T> 需要產生影像物件的類別，一般為ImageView.class或ImageButton.class
     * @param viewClass 需要產生影像物件的類別，一般為ImageView.class或ImageButton.class
     * @param bitmap 圖片的Bitmap，來源為網路或SD卡檔案時，可使用ImageUtil.loadBitmap()取得
     * @return 生成的物件
     */
    public <T extends ImageView> T createImage(Class<T> viewClass, Bitmap bitmap) {
        try {
            T iv = viewClass.getConstructor(Context.class).newInstance(context);
            designer.stylise(iv);
            iv.setImageBitmap(bitmap);
            return iv;
        } catch (Exception ex) {
            Log.e("grandroid-layout", null, ex);
            return null;
        }
    }

    /**
     * 產生一個圖片按鈕，並加入到"目前Layout"
     * 加入Layout時的參數：當"目前Layout"的Orientation橫向時為layWW(1)；其他狀況為layWW(0)
     *
     * @param <T> 需要產生影像物件的類別，一般為ImageView.class或ImageButton.class
     * @param viewClass 需要產生影像物件的類別，一般為ImageView.class或ImageButton.class
     * @param resourceID Resource ID
     * @return 生成的物件
     */
    public <T extends ImageView> T addImage(Class<T> viewClass, int resourceID) {
        T iv = createImage(viewClass, resourceID);
        lastLayout.addView(iv, layWW(lastLayout instanceof LinearLayout && ((LinearLayout) lastLayout).getOrientation() == LinearLayout.HORIZONTAL ? 1 : 0));
        return iv;
    }

    /**
     * 產生一個圖片按鈕，並加入到"目前Layout"
     * 加入Layout時的參數：當"目前Layout"的Orientation橫向時為layWW(1)；其他狀況為layWW(0)
     *
     * @param <T> 需要產生影像物件的類別，一般為ImageView.class或ImageButton.class
     * @param viewClass 需要產生影像物件的類別，一般為ImageView.class或ImageButton.class
     * @param bitmap 影像bitmap
     * @return 生成的物件
     */
    public <T extends ImageView> T addImage(Class<T> viewClass, Bitmap bitmap) {
        T iv = createImage(viewClass, bitmap);
        lastLayout.addView(iv, layWW(lastLayout instanceof LinearLayout && ((LinearLayout) lastLayout).getOrientation() == LinearLayout.HORIZONTAL ? 1 : 0));
        return iv;
    }

    /**
     *
     * @param resourceID
     * @param params
     * @return
     */
    public ImageView addImage(int resourceID, ViewGroup.LayoutParams params) {
        ImageView iv = createImage(ImageView.class, resourceID);
        lastLayout.addView(iv, params);
        return iv;
    }

    /**
     *
     * @param bitmap
     * @param params
     * @return
     */
    public ImageView addImage(Bitmap bitmap, ViewGroup.LayoutParams params) {
        ImageView iv = createImage(ImageView.class, bitmap);
        lastLayout.addView(iv, params);
        return iv;
    }

    /**
     *
     * @param btnTexts
     * @return
     */
    public RadioGroup createRadioGroup(String[] btnTexts) {
        RadioGroup rg = new RadioGroup(context);
        RadioButton[] rbs = new RadioButton[btnTexts.length];
        for (int i = 0; i < rbs.length; i++) {
            rbs[i] = new RadioButton(context);
            rbs[i].setId(i);
            designer.stylise(rbs[i]);
            rbs[i].setText(btnTexts[i]);
            rg.addView(rbs[i]);
        }
        return rg;
    }

    /**
     *
     * @param checked
     * @return
     */
    public CheckBox createCheckBox(boolean checked) {
        CheckBox cb = new CheckBox(context);
        cb.setChecked(checked);
        designer.stylise(cb);
        return cb;

    }

    /**
     *
     * @param checked
     * @return
     */
    public CheckBox addCheckBox(boolean checked) {
        CheckBox cb = createCheckBox(checked);
        lastLayout.addView(cb);
        return cb;
    }

    /**
     *
     * @param checked
     * @param text
     * @return
     */
    public CheckBox addCheckBox(boolean checked, CharSequence text) {
        CheckBox cb = createCheckBox(checked);
        cb.setText(text);
        lastLayout.addView(cb);
        //addTextView(text);
        return cb;
    }

    public ImageView addLine(int color) {
        return addLine(color, 1);
    }

    public ImageView addLine(int color, int lineWidth) {
        if (((LinearLayout) lastLayout).getOrientation() == LinearLayout.VERTICAL) {
            ImageView iv = addImage(0, lay(LinearLayout.LayoutParams.MATCH_PARENT, lineWidth, 0));
            iv.setBackgroundColor(color);
            return iv;
        } else {
            ImageView iv = addImage(0, lay(lineWidth, LinearLayout.LayoutParams.MATCH_PARENT, 0));
            iv.setBackgroundColor(color);
            return iv;
        }
    }

    /**
     * 傳回一個LayoutParams物件(W=MATCH_PARENT, H=MATCH_PARENT)
     *
     * @param width
     * @param height
     * @param weight
     * @return 生成的LinearLayout.LayoutParams物件
     */
    public LinearLayout.LayoutParams lay(int width, int height, int weight) {
        return new LinearLayout.LayoutParams(width, height, weight);
    }

    /**
     * 傳回一個LayoutParams物件(W=FILL_PARENT, H=FILL_PARENT)
     *
     * @return 生成的LinearLayout.LayoutParams物件
     */
    public LinearLayout.LayoutParams layFF() {
        return new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.MATCH_PARENT);
    }

    /**
     * 傳回一個LayoutParams物件(W=FILL_PARENT, H=FILL_PARENT)
     *
     * @param weight 權重
     * @return 生成的LinearLayout.LayoutParams物件
     */
    public LinearLayout.LayoutParams layFF(int weight) {
        return new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.MATCH_PARENT, weight);
    }

    /**
     * 傳回一個LayoutParams物件(W=FILL_PARENT, H=WRAP_CONTENT)
     *
     * @return 生成的LinearLayout.LayoutParams物件
     */
    public LinearLayout.LayoutParams layFW() {
        return layFW(0);
    }

    /**
     * 傳回一個LayoutParams物件(W=FILL_PARENT, H=WRAP_CONTENT)
     *
     * @param weight 垂直方向上的權重(水平方向上已被設為填滿，故無權重)
     * @return 生成的LinearLayout.LayoutParams物件
     */
    public LinearLayout.LayoutParams layFW(float weight) {
        if (weight > 0) {
            return new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.WRAP_CONTENT, weight);
        } else {
            return new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, LinearLayout.LayoutParams.WRAP_CONTENT);
        }
    }

    /**
     * 傳回一個LayoutParams物件(W=FILL_PARENT, H=WRAP_CONTENT)
     *
     * @param weight 垂直方向上的權重(水平方向上已被設為填滿，故無權重)
     * @return 生成的LinearLayout.LayoutParams物件
     */
    public LinearLayout.LayoutParams layWF(float weight) {
        if (weight > 0) {
            return new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.MATCH_PARENT, weight);
        } else {
            return new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.MATCH_PARENT);
        }
    }

    /**
     * 傳回一個LayoutParams物件(W=WRAP_CONTENT, H=WRAP_CONTENT, weight)
     *
     * @param weight 剩餘空間分配權重，若為0則不會被分配到剩餘空間(分配方向是看上層Layout的Orientation)
     * @return 生成的LinearLayout.LayoutParams物件
     */
    public LinearLayout.LayoutParams layWW(float weight) {
        if (weight > 0) {
            return new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.WRAP_CONTENT, weight);
        } else {
            return new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.WRAP_CONTENT);
        }
    }

    /**
     * 傳回一個LayoutParams物件，x、y、width、height的尺度是使用圖片設計的尺寸與位置，會經過m的修正
     *
     * @param x marginLeft, will be transformed by matrix
     * @param y marginTop, will be transformed by matrix
     * @param width
     * @param height
     * @return 生成的LinearLayout.LayoutParams物件
     */
    public LinearLayout.LayoutParams layAbsolute(int x, int y, int width, int height) {
        if (m != null) {
            float[] ptPos = new float[]{x, y};
            float[] ptSize = new float[]{width, height};
            if (width > 0 || height > 0) {
                m.mapPoints(ptSize);
            }
            m.mapPoints(ptPos);
            x = Math.round(ptPos[0]);
            y = Math.round(ptPos[1]);
            LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(width > 0 ? Math.round(ptSize[0]) : width, height > 0 ? Math.round(ptSize[1]) : height);
            lp.setMargins(x, y, 0, 0);
            return lp;
        } else {
            LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(width, height);
            lp.setMargins(x, y, 0, 0);
            return lp;
        }
    }

    /**
     * 傳回一個LayoutParams物件，x、y、width、height的尺度是使用圖片設計的尺寸與位置，會經過m的修正
     *
     * @param x marginLeft
     * @param y marginTop
     * @return 生成的LinearLayout.LayoutParams物件
     */
    public LinearLayout.LayoutParams layAbsolute(int x, int y) {
        return layAbsolute(x, y, LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.WRAP_CONTENT);
    }

    /**
     *
     * @param gravity
     * @return
     */
    public FrameLayout.LayoutParams layFrame(int width, int height, int gravity) {
        return new FrameLayout.LayoutParams(width, height, gravity);
    }

    /**
     *
     * @param gravity
     * @return
     */
    public FrameLayout.LayoutParams layFrameWW(int gravity) {
        return new FrameLayout.LayoutParams(FrameLayout.LayoutParams.WRAP_CONTENT, FrameLayout.LayoutParams.WRAP_CONTENT, gravity);
    }

    /**
     *
     * @param gravity
     * @return
     */
    public FrameLayout.LayoutParams layFrameFW(int gravity) {
        return new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.WRAP_CONTENT, gravity);
    }

    /**
     *
     * @param gravity
     * @return
     */
    public FrameLayout.LayoutParams layFrameWF(int gravity) {
        return new FrameLayout.LayoutParams(FrameLayout.LayoutParams.WRAP_CONTENT, FrameLayout.LayoutParams.MATCH_PARENT, gravity);
    }

    /**
     *
     * @param gravity
     * @return
     */
    public FrameLayout.LayoutParams layFrameFF() {
        return new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT);
    }

    /**
     *
     * @param gravity
     * @return
     */
    public FrameLayout.LayoutParams layFrameFF(int gravity) {
        return new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MATCH_PARENT, FrameLayout.LayoutParams.MATCH_PARENT, gravity);
    }

    /**
     * 傳回一個LayoutParams物件，x、y、width、height的尺度是使用圖片設計的尺寸與位置，會經過m的修正
     *
     * @param x marginLeft, will be transformed by matrix
     * @param y marginTop, will be transformed by matrix
     * @param width
     * @param height
     * @return 生成的FrameLayout.LayoutParams物件
     */
    public FrameLayout.LayoutParams layFrameAbsolute(int x, int y, int width, int height) {
        return layFrameAbsolute(x, y, width, height, Gravity.TOP | Gravity.LEFT);
    }

    /**
     * 傳回一個LayoutParams物件，x、y、width、height的尺度是使用圖片設計的尺寸與位置，會經過m的修正
     *
     * @param x marginLeft, will be transformed by matrix
     * @param y marginTop, will be transformed by matrix
     * @param width
     * @param height
     * @return 生成的FrameLayout.LayoutParams物件
     */
    public FrameLayout.LayoutParams layFrameAbsolute(int x, int y, int width, int height, int gravity) {
        if (m != null) {
            float[] ptPos = new float[]{x, y};
            float[] ptSize = new float[]{width, height};
            if (width > 0 || height > 0) {
                m.mapPoints(ptSize);
            }
            m.mapPoints(ptPos);
            x = Math.round(ptPos[0]);
            y = Math.round(ptPos[1]);
            FrameLayout.LayoutParams lp = new FrameLayout.LayoutParams(width > 0 ? Math.round(ptSize[0]) : width, height > 0 ? Math.round(ptSize[1]) : height, gravity);
            lp.setMargins(x, y, 0, 0);
            return lp;
        } else {
            FrameLayout.LayoutParams lp = new FrameLayout.LayoutParams(width, height, gravity);
            lp.setMargins(x, y, 0, 0);
            return lp;
        }
    }

    /**
     * 傳回一個LayoutParams物件，x、y、width、height的尺度是使用圖片設計的尺寸與位置，會經過m的修正
     *
     * @param x marginLeft
     * @param y marginTop
     * @return 生成的FrameLayout.LayoutParams物件
     */
    public FrameLayout.LayoutParams layFrameAbsolute(int x, int y) {
        return layFrameAbsolute(x, y, FrameLayout.LayoutParams.WRAP_CONTENT, FrameLayout.LayoutParams.WRAP_CONTENT);
    }

    public ViewGroup setScalablePadding(int left, int top, int right, int bottom) {
        return setScalablePadding(lastLayout, left, top, right, bottom);
    }

    /**
     * 以matrix依理想設計尺寸的padding去計算目前螢幕尺寸應使用多少Padding
     *
     * @param <T>
     * @param view
     * @param x
     * @param y
     * @return
     */
    public <T extends View> T setScalablePadding(T view, int left, int top, int right, int bottom) {
        float[] ptPos = new float[]{left, top, right, bottom};
        m.mapPoints(ptPos);
        view.setPadding(Math.round(ptPos[0]), Math.round(ptPos[1]), Math.round(ptPos[2]), Math.round(ptPos[3]));
        return view;
    }

    /**
     * 利用loadResourceImage方法將res圖讀為bitmap，再設為view的背景
     *
     * @param <T>
     * @param view
     * @param resourceID
     * @param scaleByCurrentMatrix 是否要依目前的matrix來將讀入的bitmap進行縮放
     * @return
     */
    public <T extends View> T setViewBackground(T view, int resourceID) {
        if (autoLoadResourceAsBitmap && resourceID != 0) {
            view.setBackgroundDrawable(new BitmapDrawable(loadResourceImage(resourceID)));
        } else {
            view.setBackgroundResource(resourceID);
        }
        return view;
    }

    /**
     * 產生一個橫向的LinearLayout，並加入到"目前Layout"，之後"目前Layout"將指向此新生成的LinearLayout
     * 不會帶有捲軸 加入Layout時的參數："目前Layout"已是橫向時為layWW(0)；其他則為layFW
     *
     * @return 生成的LinearLayout物件
     */
    public LinearLayout addRowLayout() {
        return addRowLayout(false);
    }

    /**
     * 產生一個橫向的LinearLayout，並加入到"目前Layout"，之後"目前Layout"將指向此新生成的LinearLayout
     * 加入Layout時的參數：withScroll=true時為layFF()；"目前Layout"已是橫向時為layWW(0)；其他則為layFW
     *
     * @param withScroll 是否要有捲軸
     * @return 生成的LinearLayout物件
     */
    public LinearLayout addRowLayout(boolean withScroll) {
        return addRowLayout(withScroll, false);
    }

    /**
     *
     * @param withScroll
     * @param fullHeight
     * @return
     */
    public LinearLayout addRowLayout(boolean withScroll, boolean fullHeight) {
        if (fullHeight) {
            if (withScroll) {
                return addRowLayout(withScroll, layFF());
            } else {
                return addRowLayout(withScroll, layWF(0));
            }
        } else {
            if (lastLayout instanceof LinearLayout && ((LinearLayout) lastLayout).getOrientation() == LinearLayout.HORIZONTAL) {
                return addRowLayout(withScroll, layWW(0));
            } else {
                return addRowLayout(withScroll, layFW());
            }
        }
    }

    /**
     *
     * @param withScroll
     * @param params
     * @return
     */
    public LinearLayout addRowLayout(boolean withScroll, ViewGroup.LayoutParams params) {
        LinearLayout ll = new LinearLayout(context);
        designer.stylise(ll);
        ll.setOrientation(LinearLayout.HORIZONTAL);

        if (withScroll) {
            HorizontalScrollView sv = new HorizontalScrollView(context);
            sv.setScrollContainer(true);
            sv.setFocusable(true);
            sv.addView(ll, layFF());
            lastLayout.addView(sv, params);
        } else {
            lastLayout.addView(ll, params);
        }
        lastLayout = ll;
        return ll;
    }

    /**
     * 產生一個直向的LinearLayout，並加入到"目前Layout"，之後"目前Layout"將指向此新生成的LinearLayout
     * 不會帶有捲軸 加入Layout時的參數為layWW(1)
     *
     * @return 生成的LinearLayout物件
     */
    public LinearLayout addColLayout() {
        return addColLayout(false);
    }

    /**
     * 產生一個直向的LinearLayout，並加入到"目前Layout"，之後"目前Layout"將指向此新生成的LinearLayout
     * 加入Layout時的參數：withScroll=true時為layFW()；否則為layWW(1)
     *
     * @param withScroll 是否要有捲軸
     * @return 生成的LinearLayout物件
     */
    public LinearLayout addColLayout(boolean withScroll) {
        return addColLayout(withScroll, false);
    }

    /**
     *
     * @param withScroll
     * @param fullHeight
     * @return
     */
    public LinearLayout addColLayout(boolean withScroll, boolean fullHeight) {
        if (fullHeight) {
            return addColLayout(withScroll, layWF(1));
        } else {
            if (lastLayout instanceof LinearLayout && ((LinearLayout) lastLayout).getOrientation() == LinearLayout.HORIZONTAL) {
                return addColLayout(withScroll, layWW(1));
            } else {
                return addColLayout(withScroll, layFW(1));
            }

        }
    }

    /**
     *
     * @param withScroll
     * @param params
     * @return
     */
    public LinearLayout addColLayout(boolean withScroll, ViewGroup.LayoutParams params) {
        LinearLayout ll = new LinearLayout(context);
        designer.stylise(ll);
        ll.setOrientation(LinearLayout.VERTICAL);
        if (withScroll) {
            ScrollView sv = new ScrollView(context);
            sv.setScrollContainer(true);
            sv.setFocusable(true);
            sv.addView(ll, new ScrollView.LayoutParams(ScrollView.LayoutParams.MATCH_PARENT, ScrollView.LayoutParams.WRAP_CONTENT));
            lastLayout.addView(sv, params);
        } else {
            lastLayout.addView(ll, params);
//            if (lastLayout instanceof LinearLayout && ((LinearLayout) lastLayout).getOrientation() == LinearLayout.HORIZONTAL) {
//                lastLayout.addView(ll, fullHeight ? layWF(1) : layWW(1));
//            } else {
//                lastLayout.addView(ll, fullHeight ? layFF() : layFW(1));
//            }
        }
        lastLayout = ll;
        return ll;
    }

    /**
     *
     * @param params
     * @return
     */
    public FrameLayout addFrame(ViewGroup.LayoutParams params) {
        FrameLayout fl = new FrameLayout(context);
        lastLayout.addView(fl, params);
        lastLayout = fl;
        return fl;
    }

    /**
     * 跳出"目前Layout"，使"目前Layout"指向其Parent(上一層)
     * 若是在呼叫了addTopBanner()或是addBottomBanner()的狀況，則會跳回先前的Layout (仍然像是上一層的概念)
     * 可連續呼叫，跳離多次後，再繼續增加其他ColLayout或RowLayout
     */
    public void escape() {
        escape(false);
    }

    /**
     * 跳出"目前Layout"，使"目前Layout"指向其Parent(上一層)
     * 若是在呼叫了addTopBanner()或是addBottomBanner()的狀況，則會跳回先前的Layout (仍然像是上一層的概念)
     * 可連續呼叫，跳離多次後，再繼續增加其他ColLayout或RowLayout
     *
     * @param force
     */
    public void escape(boolean force) {
        if (lastLayout != mainLayout) {
            ViewGroup vg = lastLayout;
            lastLayout = getParentOfLastLayout();
            if (lastLayout instanceof RelativeLayout) {
                if (force) {
                    lastLayout = getParentOfLastLayout();
                } else {
                    if (lastLayout.getChildAt(lastLayout.getChildCount() - 1) instanceof LinearLayout && lastLayout.getChildAt(lastLayout.getChildCount() - 1) != vg) {
                        lastLayout = (ViewGroup) lastLayout.getChildAt(lastLayout.getChildCount() - 1);
                    } else {
                        lastLayout = (ViewGroup) lastLayout.getChildAt(0);
                    }
                    if (!(lastLayout instanceof LinearLayout)) {
                        for (int i = 0; i < lastLayout.getChildCount(); i++) {
                            if (lastLayout.getChildAt(i) instanceof LinearLayout) {
                                lastLayout = (LinearLayout) lastLayout.getChildAt(i);
                            }
                        }
                    }
                }
            }
        }
    }

    /**
     * 對"目前Layout"設定置中對齊
     *
     * @return 目前Layout
     */
    public ViewGroup styliseCenter() {
        if (lastLayout instanceof LinearLayout) {
            ((LinearLayout) lastLayout).setGravity(Gravity.CENTER);
        }
        return lastLayout;
    }

    /**
     * 對"目前Layout"設定背景圖片，實際上會使用styliseBackground方法，並且會依目前的matrix進行縮放
     *
     * @param resourceID 背景圖片Resource ID
     * @return 目前Layout
     */
    public ViewGroup styliseBackground(int resourceID) {
        if (autoLoadResourceAsBitmap && resourceID != 0) {
            setViewBackground(lastLayout, resourceID);
        } else {
            lastLayout.setBackgroundResource(resourceID);
        }
        return lastLayout;
    }

    /**
     *
     * @param params
     * @return
     */
    public ViewGroup styliseParams(ViewGroup.LayoutParams params) {
        ViewGroup hostParent = getParentOfLastLayout();
        ViewGroup parent = (ViewGroup) lastLayout.getParent();
        if (hostParent == parent) {
            parent.removeView(lastLayout);
            parent.addView(lastLayout, params);
        } else {
            for (int i = 0; i < hostParent.getChildCount(); i++) {
                if (hostParent.getChildAt(i) == parent) {
                    hostParent.removeViewAt(i);
                    hostParent.addView(parent, i, params);
                    break;
                }
            }
        }
        return lastLayout;
    }

    /**
     *
     * @return
     */
    protected ViewGroup getParentOfLastLayout() {
        if (lastLayout.getParent() instanceof ScrollView || lastLayout.getParent() instanceof HorizontalScrollView) {
            return (ViewGroup) lastLayout.getParent().getParent();
        } else {
            return (ViewGroup) lastLayout.getParent();
        }
    }

    /**
     *
     * @return
     */
    protected ViewGroup getDecoratedLastLayout() {
        if (lastLayout.getParent() instanceof ScrollView || lastLayout.getParent() instanceof HorizontalScrollView) {
            return (ViewGroup) lastLayout.getParent();
        } else {
            return (ViewGroup) lastLayout;
        }
    }

    /**
     *
     * @param uri
     * @return
     */
    public int getResourceID(String uri) {
        return context.getResources().getIdentifier(uri, null, context.getPackageName());
    }

    public void save() {
        savedLayout = this.getLastLayout();
    }

    public void restore() {
        savedLayout.removeAllViews();
        lastLayout = savedLayout;
    }

    public void disableKeyboardFocus() {
        this.mainLayout.setFocusable(true);
        this.mainLayout.setFocusableInTouchMode(true);
        this.mainLayout.requestFocus();
    }

    public Bitmap loadResourceImage(int resId) {
        BitmapFactory.Options opt = new BitmapFactory.Options();
        opt.inPreferredConfig = Bitmap.Config.RGB_565;
        opt.inPurgeable = true;
        opt.inInputShareable = true;
        //獲取資源圖片 
        InputStream is = context.getResources().openRawResource(resId);
        Bitmap bmp = BitmapFactory.decodeStream(is, null, opt);
        if (autoScaleResource) {
            bmp = Bitmap.createBitmap(bmp, 0, 0, bmp.getWidth(), bmp.getHeight(), m, false);
        }
        return bmp;
    }

    public Bitmap loadResourceImageARGB(int resId) {
        BitmapFactory.Options opt = new BitmapFactory.Options();
        opt.inPreferredConfig = Bitmap.Config.ARGB_8888;
        opt.inPurgeable = true;
        opt.inInputShareable = true;
        //獲取資源圖片 
        InputStream is = context.getResources().openRawResource(resId);
        Bitmap bmp = BitmapFactory.decodeStream(is, null, opt);
        if (autoScaleResource) {
            bmp = Bitmap.createBitmap(bmp, 0, 0, bmp.getWidth(), bmp.getHeight(), m, false);
        }
        return bmp;
    }

    public boolean isAutoLoadResourceAsBitmap() {
        return autoLoadResourceAsBitmap;
    }

    public void setAutoLoadResourceAsBitmap(boolean autoLoadResourceAsBitmap) {
        this.autoLoadResourceAsBitmap = autoLoadResourceAsBitmap;
    }

    public boolean isAutoScaleResource() {
        return autoScaleResource;
    }

    public void setAutoScaleResource(boolean autoScaleResource) {
        this.autoScaleResource = autoScaleResource;
    }
}
