// Type definitions for DHTMLX 5.0.8
// Project: http://dhtmlx.com
// Definitions by: Anton Aksionau <http://github.com/ikari132>

type ICallable = (...args: any[]) => any;

declare type dhtmlXPortal = any;

declare type dhtmlXScheduler = any;

declare namespace dhx {
    function absLeft(obj: any): number;

    function absTop(obj: any): number;

    function attachEvent(name: string, handler: ICallable): number;

    function date2str(val: string, format: string): string;

    function detachEvent(id: number): void;

    function newId(): number;

    function s2b(value: any): boolean;

    function s2j(str: string): string;

    function screenDim(): any;

    function selectTextRange(inp: any, start: number, end: number): void;

    function str2date(val: string, format: string): string;

    function trim(str: string): string;

    const ajax: dhtmlxAjax;
    const dateFormat: boolean;
    const dateLang: boolean;
    const dateStrings: boolean;
    const isChrome: boolean;
    const isEdge: boolean;
    const isFF: boolean;
    const isIE: boolean;
    const isIE10: boolean;
    const isIE11: boolean;
    const isIE6: boolean;
    const isIE7: boolean;
    const isIE8: boolean;
    const isIE9: boolean;
    const isIPad: boolean;
    const isKHTML: boolean;
    const isOpera: boolean;
    const version: boolean;
}

type dhtmlxAjaxEventName = 'onAjaxError' | 'onLoadXMLError';

interface dhtmlxAjax {
    del(url: string, params: string, callback: ICallable): void;

    get(url: string, callback: ICallable): void;

    getSync(url: string): any;

    post(url: string, params: string, callback: ICallable): void;

    postSync(url: string, params: string): any;

    put(url: string, params: string, callback: ICallable): void;

    query(method: string, url: string, data: string, async: boolean, callback: ICallable, headers: any): void;
}

type dataProcessorEventName =
    'onAfterUpdate'
    | 'onAfterUpdateFinish'
    | 'onBeforeDataSending'
    | 'onBeforeUpdate'
    | 'onFullSync'
    | 'onRowMark'
    | 'onValidationError';

declare class dataProcessor {
    clearVerificator(index: number): void;

    defineAction(status: string, handler: ICallable): void;

    enableDataNames(mode: boolean): void;

    enableDebug(mode: boolean): void;

    enablePartialDataSend(mode: boolean): void;

    enableUTFencoding(mode: boolean): void;

    getState(id: string | number): string;

    getSyncState(): boolean;

    ignore(code: ICallable): void;

    init(obj: any): void;

    sendData(id: string | number): void;

    setTransactionMode(mode: string, total: boolean): void;

    setUpdateMode(mode: string, dnd: boolean): void;

    setUpdated(rowId: string | number, mode: boolean, state: string): void;

    setVerificator(index: number, verifyFunc: ICallable): void;

    url(): void;
}

type DataStoreEventName =
    'onAfterAdd'
    | 'onAfterCursorChange'
    | 'onAfterDelete'
    | 'onBeforeAdd'
    | 'onBeforeCursorChange'
    | 'onBeforeDelete'
    | 'onDataRequest'
    | 'onLoadError'
    | 'onStoreUpdated'
    | 'onXLE'
    | 'onXLS';

declare class DataStore {
    add(): void;

    attachEvent(name: DataStoreEventName, handler: ICallable): number;

    bind(target: any, rule: ICallable): void;

    clearAll(): void;

    dataCount(): void;

    detachEvent(id: number): void;

    exists(): void;

    filter(): void;

    first(): void;

    getCursor(): string;

    idByIndex(): void;

    indexById(): void;

    item(): void;

    last(): void;

    load(data: any, doOnLoad: ICallable): void;

    next(): void;

    parse(): void;

    previous(): void;

    remove(): void;

    saveBatch(): void;

    serialize(): void;

    setCursor(id: string): void;

    sort(): void;

    sync(target: any, rule: any): void;

    unbind(): void;

    update(): void;

}

type dhtmlXAccordionEventName =
    'onActive'
    | 'onBeforeActive'
    | 'onBeforeDrag'
    | 'onContentLoaded'
    | 'onDock'
    | 'onDrop'
    | 'onUnDock'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXAccordion {
    dhxWins: dhtmlXWindows;

    addItem(id: string | number, text: string, open: boolean, height: number, icon: string): void;

    attachEvent(name: dhtmlXAccordionEventName, handler: ICallable): number;

    attachFooter(id: any, height: number): void;

    attachHeader(id: any, height: number): void;

    attachMenu(conf: any): dhtmlXMenuObject;

    attachRibbon(conf: any): dhtmlXRibbon;

    attachStatusBar(conf: any): { [key: string]: any; };

    attachToolbar(conf: any): dhtmlXToolbarObject;

    cells(id: any): dhtmlXCell;

    clearIcon(): void;

    closeItem(): void;

    detachEvent(id: number): void;

    detachFooter(): void;

    detachHeader(): void;

    detachMenu(): void;

    detachRibbon(): void;

    detachStatusBar(): void;

    detachToolbar(): void;

    enableDND(test: any): void;

    enableMultiMode(yScrollMode: string, defaultHeight: number): void;

    forEachItem(handler: ICallable): void;

    getAttachedMenu(): dhtmlXMenuObject;

    getAttachedRibbon(): dhtmlXRibbon;

    getAttachedStatusBar(): any;

    getAttachedToolbar(): dhtmlXToolbarObject;

    getText(): void;

    hideItem(): void;

    hideMenu(): void;

    hideRibbon(): void;

    hideStatusBar(): void;

    hideToolbar(): void;

    isActive(): void;

    isItemHidden(): void;

    loadJSON(): void;

    loadStruct(data: any, doOnLoad: ICallable): void;

    loadXML(): void;

    moveOnTop(): void;

    openItem(): void;

    progressOff(): void;

    progressOn(): void;

    removeItem(id: any): void;

    setActive(): void;

    setEffect(): void;

    setIcon(): void;

    setIconsPath(path: string): void;

    setIconset(name: string): void;

    setItemHeight(): void;

    setOffset(offset: number): void;

    setOffsets(conf: any): void;

    setSizes(): void;

    setSkin(skin: string): void;

    setSkinParameters(): void;

    setText(): void;

    showItem(): void;

    showMenu(): void;

    showRibbon(): void;

    showStatusBar(): void;

    showToolbar(): void;

    unload(): void;
}

type dhtmlXCalendarObjectEventName =
    'onArrowClick'
    | 'onBeforeChange'
    | 'onButtonClick'
    | 'onChange'
    | 'onClick'
    | 'onHide'
    | 'onMouseOut'
    | 'onMouseOver'
    | 'onPopupHide'
    | 'onPopupShow'
    | 'onShow'
    | 'onTimeChange';

declare class dhtmlXCalendarObject {
    lang: string;
    langData: any;

    attachEvent(name: dhtmlXCalendarObjectEventName, handler: ICallable): number;

    attachObj(input: any): number;

    clearInsensitiveDays(): void;

    clearSensitiveRange(): void;

    clearTooltip(date: any): void;

    close(): void;

    detachEvent(id: number): void;

    detachObj(obj: any): void;

    disableDays(mode: string, date: any): void;

    draw(): void;

    enableDays(mode: string): void;

    enableIframe(mode: boolean): void;

    getCellDimension(date: Date | string): any;

    getDate(isFormatted: boolean): any;

    getFormatedDate(format: string, date: Date): string;

    getPopup(): dhtmlXPopup;

    getWeekNumber(date: Date | string): number;

    hide(): void;

    hideTime(): void;

    hideToday(): void;

    hideWeekNumbers(): void;

    isVisible(): boolean;

    loadUserLanguage(lang: string): void;

    setDate(date: Date | string): void;

    setDateFormat(format: string): void;

    setFormatedDate(format: string, date: string): void;

    setHolidays(date: any): void;

    setInsensitiveDays(date: any): void;

    setInsensitiveRange(date: any): void;

    setMinutesInterval(interval: number): void;

    setParent(id: any): void;

    setPosition(pos: string): void;

    setSensitiveRange(date: any): void;

    setSkin(skin: string): void;

    setTooltip(date: string | number, text: string, showIcon: boolean, usePopup: boolean): void;

    setWeekStartDay(day: number): void;

    setYearsRange(): void;

    show(): void;

    showMonth(date: Date | string): void;

    showTime(): void;

    showToday(): void;

    showWeekNumbers(): void;

    unload(): void;
}

type dhtmlXCarouselEventName = 'onContentLoaded' | 'onSelect';

declare class dhtmlXCarousel {
    addCell(id: any, index: number): void;

    attachEvent(name: dhtmlXCarouselEventName, handler: ICallable): number;

    attachFooter(id: any, height: number): void;

    attachHeader(id: any, height: number): void;

    attachMenu(conf: any): dhtmlXMenuObject;

    attachRibbon(conf: any): dhtmlXRibbon;

    attachStatusBar(conf: any): { [key: string]: any; };

    attachToolbar(conf: any): dhtmlXToolbarObject;

    cells(id: any): dhtmlXCell;

    detachEvent(id: number): void;

    detachFooter(): void;

    detachHeader(): void;

    detachMenu(): void;

    detachRibbon(): void;

    detachStatusBar(): void;

    detachToolbar(): void;

    enableHotKeys(mode: boolean): void;

    forEachCell(handler: ICallable): void;

    getActiveCell(): any;

    getActiveId(): any;

    getActiveIndex(): number;

    getAttachedMenu(): dhtmlXMenuObject;

    getAttachedRibbon(): dhtmlXRibbon;

    getAttachedStatusBar(): any;

    getAttachedToolbar(): dhtmlXToolbarObject;

    goFirst(): void;

    goLast(): void;

    goNext(): void;

    goPrev(): void;

    hideControls(): void;

    hideMenu(): void;

    hideRibbon(): void;

    hideStatusBar(): void;

    hideToolbar(): void;

    progressOff(): void;

    progressOn(): void;

    setCellSize(width: any, height: any): void;

    setOffset(left: number, top: number, item: number): void;

    setOffsets(conf: any): void;

    setSizes(): void;

    showControls(): void;

    showMenu(): void;

    showRibbon(): void;

    showStatusBar(): void;

    showToolbar(): void;

    unload(): void;

}

declare class dhtmlXCell {
    appendObject(id: any): void;

    attachAccordion(conf: any): dhtmlXAccordion;

    attachCarousel(width: number, height: number, conf: any): dhtmlXCarousel;

    attachChart(conf: any): dhtmlXChart;

    attachDataView(conf: any): dhtmlXDataView;

    attachEditor(): dhtmlXEditor;

    attachForm(conf: any): dhtmlXForm;

    attachGrid(): dhtmlXGridObject;

    attachHTMLString(htmlString: string): void;

    attachLayout(conf: any): dhtmlXLayoutObject;

    attachList(conf: any): dhtmlXList;

    attachMap(opts?: any): any;

    attachMenu(conf: any): dhtmlXMenuObject;

    attachObject(obj: any): void;

    attachPortal(conf: any): dhtmlXPortal;

    attachRibbon(conf: any): dhtmlXRibbon;

    attachScheduler(day: Date, mode: string, contId: string, scheduler: dhtmlXScheduler): dhtmlXScheduler;

    attachSidebar(conf: any): dhtmlXSideBar;

    attachStatusBar(conf: any): { [key: string]: any; };

    attachTabbar(conf: any): dhtmlXTabBar;

    attachToolbar(conf: any): dhtmlXToolbarObject;

    attachTree(rootId: any): dhtmlXTreeObject;

    attachTreeView(conf: any): dhtmlXTreeViewObject;

    attachURL(url: string, ajax: boolean, postData: any): void;

    attachVault(conf: any): dhtmlXVaultObject;

    detachMenu(): void;

    detachObject(remove: boolean, moveTo: string | number): void;

    detachRibbon(): void;

    detachStatusBar(): void;

    detachToolbar(): void;

    getAttachedMenu(): dhtmlXMenuObject;

    getAttachedObject(): any;

    getAttachedRibbon(): dhtmlXRibbon;

    getAttachedStatusBar(): any;

    getAttachedToolbar(): dhtmlXToolbarObject;

    getFrame(): void;

    getId(): any;

    getViewName(): string;

    hideMenu(): void;

    hideRibbon(): void;

    hideStatusBar(): void;

    hideToolbar(): void;

    progressOff(): void;

    progressOn(): void;

    reloadURL(): void;

    showInnerScroll(): void;

    showMenu(): void;

    showRibbon(): void;

    showStatusBar(): void;

    showToolbar(): void;

    showView(name: string): boolean;

    unloadView(name: string): void;

}

type dhtmlXChartEventName =
    'onAfterAdd'
    | 'onAfterDelete'
    | 'onAfterRender'
    | 'onAfterSort'
    | 'onBeforeAdd'
    | 'onBeforeContextMenu'
    | 'onBeforeDelete'
    | 'onBeforeRender'
    | 'onBeforeSort'
    | 'onItemClick'
    | 'onItemDblClick'
    | 'onLegendClick'
    | 'onMouseMove'
    | 'onMouseMoving'
    | 'onMouseOut'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXChart {
    $view: Node;

    add(obj: any): void;

    addSeries(obj: any, view: string, value: string, color: string, label: string): void;

    attachEvent(name: dhtmlXChartEventName, handler: ICallable): number;

    clearAll(): void;

    dataCount(): void;

    define(property: string, value: string): void;

    destructor(): void;

    detachEvent(id: number): void;

    exists(id: string | number): void;

    filter(key: string, value: string): void;

    first(): void;

    get(id: string | number): void;

    group(by: string, map: any): void;

    hideSeries(index: number): void;

    idByIndex(index: number): void;

    indexById(id: string | number): void;

    last(): void;

    load(data: any, doOnLoad: ICallable): void;

    next(id: string | number): void;

    parse(data: any, type: string): void;

    previous(id: string | number): void;

    refresh(): void;

    remove(id: number): void;

    render(): void;

    serialize(): any;

    set(id: string | number, hash: any): void;

    showSeries(index: number): void;

    sort(): void;

    ungroup(): void;

    update(id: string, data: any): void;
}

type dhtmlXColorPickerEventName = 'onCancel' | 'onChange' | 'onHide' | 'onSaveColor' | 'onSelect' | 'onShow';

declare class dhtmlXColorPicker {
    dhtmlxColorPickerLangModules: boolean;

    attachEvent(name: dhtmlXColorPickerEventName, handler: ICallable): number;

    close(): void;

    detachEvent(id: number): void;

    dhtmlXColorPickerInput(inputs: any[]): void;

    getCustomColors(): any[];

    getSelectedColor(): any[];

    hide(): void;

    hideMemory(): void;

    hideOnSelect(flag: boolean): void;

    init(): void;

    isVisible(): boolean;

    linkTo(obj1: any, obj2: any, obj3: any): void;

    loadUserLanguage(lang: string): void;

    setColor(color: any): void;

    setCustomColors(color: any): void;

    setImagePath(): void;

    setOnCancelHandler(func: ICallable): void;

    setOnSelectHandler(func: ICallable): void;

    setPosition(x: number, y: number): void;

    setSkin(name: string): void;

    show(): void;

    showMemory(): void;

    unload(): void;
}

type dhtmlXComboEventName =
    'onBeforeCheck'
    | 'onBlur'
    | 'onChange'
    | 'onCheck'
    | 'onClose'
    | 'onDynXLS'
    | 'onFocus'
    | 'onKeyPressed'
    | 'onOpen'
    | 'onSelectionChange'
    | 'onSyncApply'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXCombo {
    DOMParent: boolean;
    DOMelem: boolean;
    DOMelem_button: boolean;
    DOMelem_input: boolean;
    DOMlist: boolean;

    addOption(options: any): void;

    allowFreeText(state: boolean): void;

    attachChildCombo(): void;

    attachEvent(name: dhtmlXComboEventName, handler: ICallable): number;

    clearAll(hideList: boolean): void;

    closeAll(): void;

    confirmValue(): void;

    deleteOption(value: string): void;

    destructor(): void;

    detachEvent(id: number): void;

    disable(mode: boolean): void;

    disableAutocomplete(): void;

    enable(mode: boolean): void;

    enableAutocomplete(): void;

    enableFilteringMode(mode: string | number, url: string, cache: boolean, autoSubLoad: boolean): void;

    enableOptionAutoHeight(flag: boolean, maxHeight: number): void;

    enableOptionAutoPositioning(flag: boolean): void;

    enableOptionAutoWidth(flag: boolean): void;

    filter(handler: ICallable, showList: boolean): void;

    forEachOption(handler: ICallable): void;

    getActualValue(): any;

    getBase(): HTMLElement;

    getButton(): HTMLElement;

    getChecked(index: number): any[];

    getComboText(): string;

    getIndexByValue(value: any): number;

    getInput(): HTMLElement;

    getList(): HTMLElement;

    getOption(value: string): any;

    getOptionByIndex(ind: number): any;

    getOptionByLabel(label: string): any;

    getOptionsCount(): number;

    getParent(): HTMLElement;

    getSelectedIndex(): number;

    getSelectedText(): string;

    getSelectedValue(): any;

    hide(): void;

    isChecked(index: number): boolean;

    isEnabled(): boolean;

    isVisible(): boolean;

    load(data: any, doOnLoad: ICallable): void;

    loadXML(url: string): void;

    loadXMLString(string: string): void;

    openSelect(): void;

    readonly(mode: boolean): void;

    render(mode: boolean): void;

    selectOption(ind: number, filter: boolean, conf: boolean): void;

    setAutoSubCombo(): void;

    setChecked(index: number, state: boolean): void;

    setComboText(text: string): void;

    setComboValue(value: string): void;

    setDefaultImage(url: string): void;

    setFilterHandler(handler: ICallable): void;

    setFocus(): void;

    setFontSize(sizeInp: string, sizeList: string): void;

    setImagePath(path: string): void;

    setName(name: string): void;

    setOptionHeight(height: number): void;

    setOptionIndex(value: string, index: number): void;

    setOptionWidth(width: number): void;

    setPlaceholder(text: string): void;

    setSize(new_size: number): void;

    setSkin(skin: string): void;

    setTemplate(data: any): void;

    show(mode: boolean): void;

    sort(mode: any): void;

    unSelectOption(): void;

    unload(): void;

    updateOption(oldvalue: string, avalue: string, atext: string, accs: string): void;
}

type dhtmlXDataViewEventName =
    'onAfterAdd'
    | 'onAfterDelete'
    | 'onAfterDrop'
    | 'onAfterEditStart'
    | 'onAfterEditStop'
    | 'onAfterRender'
    | 'onAfterSelect'
    | 'onBeforeAdd'
    | 'onBeforeContextMenu'
    | 'onBeforeDelete'
    | 'onBeforeDrag'
    | 'onBeforeDragIn'
    | 'onBeforeDrop'
    | 'onBeforeEditStart'
    | 'onBeforeEditStop'
    | 'onBeforeRender'
    | 'onBeforeSelect'
    | 'onDataRequest'
    | 'onDragOut'
    | 'onEditKeyPress'
    | 'onItemClick'
    | 'onItemDblClick'
    | 'onItemRender'
    | 'onMouseMove'
    | 'onMouseMoving'
    | 'onMouseOut'
    | 'onSelectChange'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXDataView {
    $view: Node;

    add(obj: any, index: number): void;

    attachEvent(name: dhtmlXDataViewEventName, handler: ICallable): number;

    changeId(oldId: string, newId: string): void;

    clearAll(): void;

    copy(sid: any, tindex: number, tobj: any, tid: any): void;

    customize(properties: any): void;

    dataCount(): number;

    define(mode: string, value: boolean): void;

    destructor(): void;

    detachEvent(id: number): void;

    edit(id: any): void;

    exists(id: any): boolean;

    filter(key: string | ICallable, value: string): void;

    first(): string | number;

    get(id: any): any;

    getSelected(as_array: boolean): any;

    idByIndex(index: number): string | number;

    indexById(ID: any): number;

    isEdit(): string;

    isSelected(id: any): void;

    last(): string | number;

    load(data: any, doOnLoad: ICallable): void;

    locate(ev: Event): void;

    move(sid: any, tindex: number, tobj: any, tid: any): void;

    moveBottom(id: any): void;

    moveDown(id: any, step: number): void;

    moveTop(id: any): void;

    moveUp(id: any, step: number): void;

    next(id: any): string | number;

    parse(obj: string | { [key: string]: any; }, type: string): void;

    previous(id: any): string | number;

    refresh(id: any): void;

    remove(id: any): void;

    select(id: any): void;

    selectAll(): void;

    serialize(): any;

    set(id: any, hash: any): void;

    show(id: any): void;

    sort(key: string, direction: string): void;

    stopEdit(): void;

    unselect(id: any): void;

    unselectAll(): void;

    update(id: string, data: any): void;
}

type dhtmlXEditorEventName = 'onAccess' | 'onContentSet' | 'onFocusChanged' | 'onToolbarClick';

declare class dhtmlXEditor {
    alignCenter(): void;

    alignJustify(): void;

    alignLeft(): void;

    alignRight(): void;

    applyBold(): void;

    applyH1(): void;

    applyH2(): void;

    applyH3(): void;

    applyH4(): void;

    applyItalic(): void;

    applyStrikethrough(): void;

    applySub(): void;

    applySuper(): void;

    applyUnderscore(): void;

    attachEvent(name: dhtmlXEditorEventName, handler: ICallable): number;

    clearFormatting(): void;

    createBulList(): void;

    createNumList(): void;

    decreaseIndent(): void;

    detachEvent(id: number): void;

    getContent(): HTMLElement;

    increaseIndent(): void;

    setContent(html: string): void;

    setContentHTML(url: string): void;

}

type dhtmlXFileUploaderEventName =
    'onBeforeClear'
    | 'onBeforeFileAdd'
    | 'onBeforeFileRemove'
    | 'onClear'
    | 'onFileAdd'
    | 'onFileRemove'
    | 'onUploadCancel'
    | 'onUploadComplete'
    | 'onUploadFail'
    | 'onUploadFile';

declare class dhtmlXFileUploader {
    clear(): void;

    enableTitleScreen(mode: boolean): void;

    getData(): any;

    getStatus(name: string): number;

    setAutoRemove(mode: boolean): void;

    setAutoStart(mode: boolean): void;

    setSLURL(slUrl: string): void;

    setSWFURL(swfUrl: string): void;

    setTitleText(text: string): void;

    setURL(url: string): void;

    upload(): void;

}

type dhtmlXFormEventName =
    'onAfterReset'
    | 'onAfterSave'
    | 'onAfterValidate'
    | 'onBeforeChange'
    | 'onBeforeClear'
    | 'onBeforeDataLoad'
    | 'onBeforeFileAdd'
    | 'onBeforeFileRemove'
    | 'onBeforeReset'
    | 'onBeforeSave'
    | 'onBeforeValidate'
    | 'onBlur'
    | 'onButtonClick'
    | 'onChange'
    | 'onClear'
    | 'onDisable'
    | 'onEditorAccess'
    | 'onEditorToolbarClick'
    | 'onEnable'
    | 'onEnter'
    | 'onFileAdd'
    | 'onFileRemove'
    | 'onFocus'
    | 'onInfo'
    | 'onInputChange'
    | 'onKeydown'
    | 'onKeyup'
    | 'onOptionsLoaded'
    | 'onUploadCancel'
    | 'onUploadComplete'
    | 'onUploadFail'
    | 'onUploadFile'
    | 'onValidateError'
    | 'onValidateSuccess'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXForm {
    addItem(pId: any, itemData: any, pos: number, insertAfter: number): void;

    adjustParentSize(): void;

    attachEvent(name: dhtmlXFormEventName, handler: ICallable): number;

    checkItem(name: string, value: any): void;

    clear(): void;

    clearBackup(id: any): void;

    clearNote(name: string, value: any): void;

    clearValidation(name: string, value: any): void;

    detachEvent(id: number): void;

    disableItem(name: string, value: any): void;

    enableItem(name: string, value: any): void;

    enableLiveValidation(state: boolean): void;

    forEachItem(handler: ICallable): void;

    getCalendar(name: string): any;

    getCheckedValue(name: string): any;

    getColorPicker(name: string): any;

    getColumnNode(pId: any, index: number): HTMLElement;

    getCombo(name: string): any;

    getContainer(name: string): any;

    getEditor(name: string): any;

    getFirstActive(): string;

    getForm(): any;

    getFormData(asString: boolean): any;

    getInput(name: string): any;

    getItemLabel(name: any, value: any): string;

    getItemText(): void;

    getItemType(name: string, value: any): string;

    getItemValue(name: string): any;

    getItemWidth(name: string): number;

    getItemsList(): void;

    getOptions(name: string): any;

    getSelect(name: string): any;

    getUploader(name: string): any;

    getUploaderStatus(name: string): number;

    getUserData(name: string, udKey: any): any;

    hideItem(name: string, value: any): void;

    isItem(name: string, value: any): boolean;

    isItemChecked(name: string, value: any): boolean;

    isItemEnabled(name: string, value: any): boolean;

    isItemHidden(name: string, value: any): boolean;

    isLocked(): boolean;

    isReadonly(name: string): boolean;

    load(data: any, doOnLoad: ICallable): void;

    loadStruct(data: any, doOnLoad: ICallable): void;

    loadStructString(): void;

    lock(): void;

    reloadOptions(name: string, data: any): void;

    removeColumn(pId: string | number, index: number, removeItems: boolean, moveAfter: boolean): void;

    removeItem(name: string, value: any): void;

    reset(): void;

    resetDataProcessor(mode: string): void;

    resetValidateCss(name: string): void;

    restoreBackup(id: any): void;

    save(): void;

    saveBackup(): void;

    send(url: string, mode: string, callback: ICallable, skipValidation: boolean): void;

    setCalendarDateFormat(name: string, dateFormat: string, serverDateFormat: string): void;

    setFocusOnFirstActive(): void;

    setFontSize(size: any): void;

    setFormData(data: any): void;

    setItemFocus(name: string): void;

    setItemHeight(name: string, height: number): void;

    setItemLabel(name: string, value: any, label: string): void;

    setItemText(): void;

    setItemValue(name: string, value: any): void;

    setItemWidth(name: string, width: number): void;

    setNote(name: string, value: any, note: any): void;

    setNumberFormat(name: string, format: string, groupSep: string, decSep: string): void;

    setReadonly(name: string, state: boolean): void;

    setRequired(name: string, value: string | number, state: boolean): void;

    setSkin(skin: string): void;

    setTooltip(itemId: any, value: any, tooltip: string): void;

    setUserData(name: string, udKey: any, udValue: string): void;

    setValidateCss(name: string, state: boolean, custom: string): void;

    setValidation(name: string, value: any, rule: any): void;

    showItem(name: string, value: any): void;

    uncheckItem(name: string, value: any): void;

    unload(): void;

    unlock(): void;

    updateValues(): void;

    validate(): void;

    validateItem(name: string): void;

}

type dhtmlXGridObjectEventName =
    'onAfterCMove'
    | 'onAfterRowDeleted'
    | 'onAfterSorting'
    | 'onBeforeBlockSelected'
    | 'onBeforeCMove'
    | 'onBeforeContextMenu'
    | 'onBeforeDrag'
    | 'onBeforeFormSubmit'
    | 'onBeforePageChanged'
    | 'onBeforeRowDeleted'
    | 'onBeforeSelect'
    | 'onBeforeSorting'
    | 'onBlockRightClick'
    | 'onBlockSelected'
    | 'onCalendarShow'
    | 'onCellChanged'
    | 'onCellMarked'
    | 'onCellUnMarked'
    | 'onCheck'
    | 'onCheckbox'
    | 'onClearAll'
    | 'onCollectValues'
    | 'onColumnCollapse'
    | 'onColumnHidden'
    | 'onDataReady'
    | 'onDhxCalendarCreated'
    | 'onDistributedEnd'
    | 'onDrag'
    | 'onDragIn'
    | 'onDragOut'
    | 'onDrop'
    | 'onDynXLS'
    | 'onEditCancel'
    | 'onEditCell'
    | 'onEmptyClick'
    | 'onEnter'
    | 'onFilterEnd'
    | 'onFilterStart'
    | 'onGridReconstructed'
    | 'onGroup'
    | 'onGroupClick'
    | 'onGroupStateChanged'
    | 'onHeaderClick'
    | 'onKeyPress'
    | 'onLastRow'
    | 'onLiveValidationCorrect'
    | 'onLiveValidationError'
    | 'onMouseOver'
    | 'onPageChanged'
    | 'onPaging'
    | 'onResize'
    | 'onResizeEnd'
    | 'onRightClick'
    | 'onRowAdded'
    | 'onRowCreated'
    | 'onRowDblClicked'
    | 'onRowHide'
    | 'onRowIdChange'
    | 'onRowInserted'
    | 'onRowPaste'
    | 'onRowSelect'
    | 'onScroll'
    | 'onSelectStateChanged'
    | 'onStatReady'
    | 'onSubAjaxLoad'
    | 'onSubGridCreated'
    | 'onSubRowOpen'
    | 'onSyncApply'
    | 'onTab'
    | 'onUndo'
    | 'onUnGroup'
    | 'onValidationCorrect'
    | 'onValidationError'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXGridObject {
    csvParser: any;
    editor: any;

    addRow(new_id: string | number, text: string | number, ind: string | number): void;

    addRowFromClipboard(): void;

    adjustColumnSize(cInd: number): void;

    attachEvent(evName: dhtmlXGridObjectEventName, evHandler: ICallable): void;

    attachFooter(values: any[], style: any[]): void;

    attachHeader(values: any[], style?: any[]): void;

    attachToObject(obj: any): void;

    cellById(row_id: string | number, col_ind: number): void;

    cellByIndex(row_ind: number, col_ind: number): void;

    cellToClipboard(rowId: string | number, cellInd: number): void;

    cells(row_id: string | number, col: number): void;

    cells2(row_index: number, col: number): void;

    changePage(pageNum: number): void;

    changePageRelative(ind: number): void;

    changeRowId(oldRowId: string | number, newRowId: string | number): void;

    checkAll(mode: boolean): void;

    clearAll(header: boolean): void;

    clearAndLoad(url: string, call: ICallable, type: string): void;

    clearChangedState(): void;

    clearConfigCookie(name: string): void;

    clearSelection(): void;

    collapseAllGroups(): void;

    collapseColumns(cInd: number): void;

    collapseGroup(val: string): void;

    collectValues(column: number): any[];

    copyBlockToClipboard(): void;

    copyRowContent(from_row: string | number, to_row_id: string | number): void;

    deleteColumn(ind: number): void;

    deleteRow(row_id: string | number): void;

    deleteSelectedRows(): void;

    destructor(): void;

    detachEvent(id: string): void;

    detachFooter(index: number): void;

    detachHeader(index: number): void;

    disableUndoRedo(): void;

    doRedo(): void;

    doUndo(): void;

    doesRowExist(row_id: string | number): void;

    editCell(): void;

    editStop(ode: boolean): void;

    enableAccessKeyMap(): void;

    enableAlterCss(cssE: string, cssU: string, perLevel: boolean, levelUnique: boolean): void;

    enableAutoHeight(mode: boolean, maxHeight: number, countFullHeight: boolean): void;

    enableAutoHiddenColumnsSaving(name: string, cookie_param: string): void;

    enableAutoSaving(name: string, cookie_param: string): void;

    enableAutoSizeSaving(name: string, cookie_param: string): void;

    enableAutoWidth(mode: boolean, max_limit: number, min_limit: number): void;

    enableBlockSelection(mode: boolean): void;

    enableCSVAutoID(mode: boolean): void;

    enableCSVHeader(mode: boolean): void;

    enableCellIds(mode: boolean): void;

    enableColSpan(mode: boolean): void;

    enableColumnAutoSize(mode: boolean): void;

    enableColumnMove(mode: boolean, columns: string): void;

    enableContextMenu(menu: any): void;

    enableDistributedParsing(mode: boolean, count: number, time: number): void;

    enableDragAndDrop(mode: boolean): void;

    enableDragOrder(mode: any): void;

    enableEditEvents(click: boolean, dblclick: boolean, f2Key: boolean): void;

    enableEditTabOnly(state: boolean): void;

    enableExcelKeyMap(): void;

    enableHeaderImages(mode: boolean): void;

    enableHeaderMenu(list: string): void;

    enableKeyboardSupport(mode: boolean): void;

    enableLightMouseNavigation(mode: boolean): void;

    enableMarkedCells(mode: boolean): void;

    enableMathEditing(mode: boolean): void;

    enableMathSerialization(mode: boolean): void;

    enableMercyDrag(mode: boolean): void;

    enableMultiline(state: boolean): void;

    enableMultiselect(state: boolean): void;

    enableOrderSaving(name: string, cookie_param: string): void;

    enablePaging(mode: boolean, pageSize: number, pagesInGrp: number, pagingControlsContainer: number | HTMLElement, showRecInfo: boolean, pagingStateContainer: number | HTMLElement): void;

    enablePreRendering(buffer: number): void;

    enableResizing(list: string): void;

    enableRowsHover(mode: boolean, cssClass: string): void;

    enableRowspan(): void;

    enableSmartRendering(mode: boolean, buffer: number): void;

    enableSortingSaving(name: string, cookie_param: string): void;

    enableStableSorting(mode: boolean): void;

    enableTooltips(list: string): void;

    enableUndoRedo(): void;

    enableValidation(mode: boolean): void;

    expandAllGroups(): void;

    expandColumns(cInd: number): void;

    expandGroup(val: string): void;

    filterBy(column: number, value: string, preserve: boolean): void;

    filterByAll(): void;

    findCell(value: string, c_ind: number, first: boolean): void;

    forEachCell(rowId: any, custom_code: ICallable): void;

    forEachRow(custom_code: ICallable): void;

    forEachRowInGroup(name: string, custom_code: ICallable): void;

    forceFullLoading(buffer: number): void;

    forceLabelSelection(mode: boolean): void;

    getAllRowIds(separator: string): string;

    getChangedRows(nd_added: boolean): string;

    getCheckedRows(col_ind: number): string;

    getColIndexById(id: number): number;

    getColLabel(cin: number, ind: number): string;

    getColType(cInd: number): string;

    getColTypeById(cID: any): string;

    getColWidth(ind: number): number;

    getColumnCombo(column_index: number): any;

    getColumnId(cin: number): any;

    getColumnLabel(cin: number, ind: number): string;

    getColumnsNum(): number;

    getCombo(col_ind: number): any;

    getCustomCombo(id: any, ind: number): any;

    getFilterElement(index: number): any;

    getFooterLabel(cin: number, ind: number, mode: boolean): string;

    getHeaderMenu(columns: any): any;

    getMarked(): any[];

    getRedo(): any[];

    getRowAttribute(rId: any, name: string): any;

    getRowId(ind: number): any;

    getRowIndex(row_id: any): number;

    getRowsNum(): number;

    getSelectedBlock(): any;

    getSelectedCellIndex(): number;

    getSelectedRowId(): any;

    getSortingState(): string;

    getStateOfView(): any[];

    getUndo(): any[];

    getUserData(row_id: any, name: any): any;

    gridFromClipboard(): void;

    gridToClipboard(): void;

    gridToGrid(rowId: any, sgrid: any, tgrid: any): void;

    gridToTreeElement(treeObj: any, treeNodeId: any, gridRowId: any): void;

    groupBy(ind: number, mask: any[]): void;

    groupStat(key: string, ind: number, item: string): number;

    init(): void;

    insertColumn(ind: number, header: string, type: string, width: number, sort: string, align: string, valign: string, reserved: any, columnColor: string): void;

    isColumnHidden(ind: number): void;

    load(url: string, call: ICallable, type: string): void;

    loadHiddenColumnsFromCookie(name: string): void;

    loadOpenStates(name: string): void;

    loadOrderFromCookie(name: string): void;

    loadSizeFromCookie(name: string): void;

    loadSortingFromCookie(name: string): void;

    lockRow(rowId: any, mode: boolean): void;

    makeFilter(id: number | HTMLElement, column: number, preserve: boolean): void;

    makeSearch(id: any, column: number): void;

    mark(row: string | number, cInd: number, state: boolean): void;

    moveColumn(oldInd: number, newInd: number): void;

    moveRow(rowId: any, mode: string, targetId: any, targetGrid: any): void;

    moveRowDown(row_id: any): void;

    moveRowTo(srowId: any, trowId: any, mode: string, dropmode: string, sourceGrid: any, targetGrid: any): void;

    moveRowUp(row_id: any): void;

    parse(data: string | { [key: string]: any; }, type: string): void;

    pasteBlockFromClipboard(): void;

    post(url: string, post: string, call: ICallable, type: string): void;

    preventIECaching(mode: boolean): void;

    printView(before: string, after: string): void;

    refreshComboColumn(index: number): void;

    refreshFilters(): void;

    refreshMath(): void;

    registerCList(col: number, list: any[]): void;

    rowToClipboard(rowId: any): void;

    rowToDragElement(id: any): void;

    saveHiddenColumnsToCookie(name: string, cookie_param: string): void;

    saveOpenStates(name: string): void;

    saveOrderToCookie(name: string, cookie_param: string): void;

    saveSizeToCookie(name: string, cookie_param: string): void;

    saveSortingToCookie(name: string, cookie_param: string): void;

    selectAll(): void;

    selectBlock(start_row: string | number, start_col: number, end_row: string | number, end_column: number): void;

    selectCell(row: number | HTMLElement, cInd: number, preserve: boolean, edit: boolean, show: boolean): void;

    selectRow(row: number | HTMLElement, fl: boolean, preserve: boolean, show: boolean): void;

    selectRowById(row_id: string | number, preserve: boolean, show: boolean, call: boolean): void;

    serialize(): void;

    serializeToCSV(text_only: boolean): void;

    setActive(mode: boolean): void;

    setAwaitedRowHeight(height: number): void;

    setCSVDelimiter(str: string): void;

    setCellExcellType(rowId: any, cellIndex: number, type: string): void;

    setCellTextStyle(row_id: any, ind: number, styleString: string): void;

    setCheckedRows(col_ind: number, v: number): void;

    setColAlign(alStr: string): void;

    setColLabel(col: number, ind: number): void;

    setColSorting(sortStr: string): void;

    setColTypes(typeStr: string): void;

    setColVAlign(valStr: string): void;

    setColValidators(vals: string): void;

    setColWidth(ind: number, value: string): void;

    setColspan(row_id: string | number, col_index: number, colspan: number): void;

    setColumnColor(clr: string): void;

    setColumnExcellType(colIndex: number, type: string): void;

    setColumnHidden(ind: number, state: boolean): void;

    setColumnId(ind: number, id: any): void;

    setColumnIds(ids: string): void;

    setColumnLabel(col: number, ind: number): void;

    setColumnMinWidth(width: number, ind: number): void;

    setColumnsVisibility(list: string): void;

    setCustomSorting(func: ICallable, col: number): void;

    setDateFormat(mask: string, server_mask: string): void;

    setDelimiter(delim: string): void;

    setDragBehavior(mode: string): void;

    setEditable(mode: boolean): void;

    setExternalTabOrder(start: any, end: any): void;

    setFieldName(name: string): void;

    setFooterLabel(col: number, label: string, ind: number): void;

    setHeader(hdrStr: string, splitSign?: string, styles?: any[]): void;

    setIconsPath(path: string): void;

    setIconset(name: string): void;

    setImagesPath(path: string): void;

    setInitWidths(wp: string): void;

    setInitWidthsP(wp: string): void;

    setMathRound(digits: number): void;

    setNoHeader(fl: boolean): void;

    setNumberFormat(mask: string, cInd: number, p_sep: string, d_sep: string): void;

    setPagingSkin(name: string): void;

    setPagingTemplates(navigation_template: string, info_template: string): void;

    setPagingWTMode(navButtons: boolean, navLabel: boolean, pageSelect: boolean, perPageSelect: boolean | any[]): void;

    setRowAttribute(id: any, name: string, value: any): void;

    setRowColor(row_id: any, color: string): void;

    setRowExcellType(rowId: any, type: string): void;

    setRowHidden(id: string | number, state: boolean): void;

    setRowId(ind: number, row_id: any): void;

    setRowTextBold(row_id: any): void;

    setRowTextNormal(row_id: any): void;

    setRowTextStyle(row_id: any, styleString: string): void;

    setRowspan(rowID: any, colInd: number, length: number): void;

    setSerializableColumns(list: string): void;

    setSerializationLevel(userData: boolean, selectedAttr: boolean, config: boolean, changedAttr: boolean, onlyChanged: boolean, asCDATA: boolean): void;

    setSizes(): void;

    setSkin(name: string): void;

    setSortImgState(state: boolean, ind: number, order: string, row: number): void;

    setStyle(ss_header: string, ss_grid: string, ss_selCell: string, ss_selRow: string): void;

    setSubGrid(subgrid: any, sInd: number, tInd: number): void;

    setSubTree(subgrid: any, sInd: number): void;

    setTabOrder(order: string): void;

    setUserData(row_id: any, name: string, value: any): void;

    setXMLAutoLoading(url: string, buffer: number): void;

    showRow(rowID: any): void;

    sortRows(col: number, type: string, order: string): void;

    splitAt(ind: number): void;

    startFastOperations(): void;

    stopFastOperations(): void;

    submitAddedRows(mode: boolean): void;

    submitColumns(inds: string): void;

    submitOnlyChanged(mode: boolean): void;

    submitOnlyRowID(mode: boolean): void;

    submitOnlySelected(mode: boolean): void;

    submitSerialization(mode: boolean): void;

    toExcel(path: string): void;

    toPDF(path: any): void;

    treeToGridElement(treeObj: any, treeNodeId: any, gridRowId: any): void;

    uid(): void;

    unGroup(): void;

    uncheckAll(): void;

    unmarkAll(): void;

    updateCellFromClipboard(rowId: any, cellInd: number): void;

    updateFromXML(url: string, insert_new: boolean, del_missed: boolean, afterCall: ICallable): void;

    updateGroups(): void;

    updateRowFromClipboard(rowId: any): void;

    validateCell(id: any, index: number, rule: ICallable): void;
}

type dhtmlXLayoutObjectEventName =
    'onCollapse'
    | 'onContentLoaded'
    | 'onDblClick'
    | 'onDock'
    | 'onExpand'
    | 'onPanelResizeFinish'
    | 'onResize'
    | 'onResizeFinish'
    | 'onUndock';

declare class dhtmlXLayoutObject {
    dhxWins: dhtmlXWindows;
    items: any[];

    constructor(config: any);

    attachEvent(name: dhtmlXLayoutObjectEventName, handler: ICallable): number;

    attachFooter(id: any, height: number): void;

    attachHeader(id: any, height: number): void;

    attachMenu(conf: any): dhtmlXMenuObject;

    attachRibbon(conf: any): dhtmlXRibbon;

    attachStatusBar(conf: any): { [key: string]: any; };

    attachToolbar(conf: any): dhtmlXToolbarObject;

    cells(id: string): dhtmlXCell;

    detachEvent(id: number): void;

    detachFooter(): void;

    detachHeader(): void;

    detachMenu(): void;

    detachRibbon(): void;

    detachStatusBar(): void;

    detachToolbar(): void;

    dockWindow(): void;

    forEachItem(handler: ICallable): void;

    getAttachedMenu(): dhtmlXMenuObject;

    getAttachedRibbon(): dhtmlXRibbon;

    getAttachedStatusBar(): any;

    getAttachedToolbar(): dhtmlXToolbarObject;

    getEffect(): void;

    getIdByIndex(): void;

    getIndexById(): void;

    hideMenu(): void;

    hidePanel(): void;

    hideRibbon(): void;

    hideStatusBar(): void;

    hideToolbar(): void;

    isPanelVisible(): void;

    listAutoSizes(): void;

    listPatterns(): void;

    listViews(): void;

    progressOff(): void;

    progressOn(): void;

    setAutoSize(hor: string, ver: string): void;

    setCollapsedText(): void;

    setEffect(): void;

    setImagePath(): void;

    setOffsets(conf: any): void;

    setSeparatorSize(index: number, size: number): void;

    setSizes(): void;

    setSkin(skin: string): void;

    showMenu(): void;

    showPanel(): void;

    showRibbon(): void;

    showStatusBar(): void;

    showToolbar(): void;

    unDockWindow(): void;

    unload(): void;
}

type dhtmlXListEventName =
    'onAfterAdd'
    | 'onAfterDelete'
    | 'onAfterDrop'
    | 'onAfterEditStart'
    | 'onAfterEditStop'
    | 'onAfterRender'
    | 'onAfterSelect'
    | 'onBeforeAdd'
    | 'onBeforeContextMenu'
    | 'onBeforeDelete'
    | 'onBeforeDrag'
    | 'onBeforeDragIn'
    | 'onBeforeDrop'
    | 'onBeforeEditStart'
    | 'onBeforeEditStop'
    | 'onBeforeRender'
    | 'onBeforeSelect'
    | 'onDataRequest'
    | 'onDragOut'
    | 'onEditKeyPress'
    | 'onItemClick'
    | 'onItemDblClick'
    | 'onItemRender'
    | 'onMouseMove'
    | 'onMouseMoving'
    | 'onMouseOut'
    | 'onSelectChange'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXList {
    $view: Node;

    add(obj: any, index: number): void;

    attachEvent(name: dhtmlXListEventName, handler: ICallable): number;

    changeId(oldId: string, newId: string): void;

    clearAll(): void;

    copy(sid: any, tindex: number, tobj: any, tid: any): void;

    customize(properties: any): void;

    dataCount(): number;

    define(mode: string, value: boolean): void;

    destructor(): void;

    detachEvent(id: number): void;

    edit(id: any): void;

    exists(id: any): boolean;

    filter(key: string | ICallable, value: string): void;

    first(): string | number;

    get(id: any): any;

    getSelected(as_array: boolean): any;

    idByIndex(index: number): string | number;

    indexById(ID: any): number;

    isEdit(): string;

    isSelected(id: any): void;

    last(): string | number;

    load(data: any, doOnLoad: ICallable): void;

    locate(ev: Event): void;

    move(sid: any, tindex: number, tobj: any, tid: any): void;

    moveBottom(id: any): void;

    moveDown(id: any, step: number): void;

    moveTop(id: any): void;

    moveUp(id: any, step: number): void;

    next(id: any): string | number;

    parse(obj: string | { [key: string]: any; }, type: string): void;

    previous(id: any): string | number;

    refresh(id: any): void;

    remove(id: any): void;

    select(id: any): void;

    selectAll(): void;

    serialize(): any;

    set(id: any, hash: any): void;

    show(id: any): void;

    sort(key: string, direction: string): void;

    stopEdit(): void;

    unselect(id: any): void;

    unselectAll(): void;

    update(id: string, data: any): void;
}

type dhtmlXMenuObjectEventName =
    'onAfterContextMenu'
    | 'onBeforeContextMenu'
    | 'onCheckboxClick'
    | 'onClick'
    | 'onContextMenu'
    | 'onHide'
    | 'onRadioClick'
    | 'onShow'
    | 'onTouch'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXMenuObject {
    addCheckbox(mode: string, nextToId: any, pos: number, itemId: string | number, text: string, state: boolean, dis: boolean): void;

    addContextZone(zoneId: any): void;

    addNewChild(parId: any, pos: number, itemId: string | number, text: string, dis: boolean, imgEn: string, imgDis: string): void;

    addNewSeparator(nextToId: any, itemId: any): void;

    addNewSibling(nextToId: string | number, itemId: string | number, text: string, dis: boolean, imgEn: string, imgDis: string): void;

    addRadioButton(mode: string, nextToId: string | number, pos: number, itemId: string | number, text: string, group: string | number, state: boolean, dis: boolean): void;

    attachEvent(name: dhtmlXMenuObjectEventName, handler: ICallable): number;

    clearAll(): void;

    clearHref(itemId: any): void;

    clearItemImage(itemId: any): void;

    detachEvent(id: number): void;

    enableDynamicLoading(url: string, icon: boolean): void;

    enableEffect(name: string, maxOpacity: number, effectSpeed: number): void;

    forEachItem(handler: ICallable): void;

    getCheckboxState(id: any): boolean;

    getCircuit(id: any): any[];

    getContextMenuHideAllMode(): boolean;

    getHotKey(itemId: any): string;

    getItemImage(itemId: any): any[];

    getItemPosition(itemId: any): number;

    getItemText(itemId: any): string;

    getItemType(itemId: string): any;

    getParentId(itemId: any): any;

    getRadioChecked(group: string): any;

    getTooltip(itemId: any): string;

    getUserData(itemId: any, name: string): any;

    hide(): void;

    hideContextMenu(): void;

    hideItem(id: any): void;

    isContextZone(zoneId: any): boolean;

    isItemEnabled(itemId: any): boolean;

    isItemHidden(itemId: any): boolean;

    loadFromHTML(object: HTMLElement, clearAfterAdd: boolean, onLoadFunction: ICallable): void;

    loadStruct(data: any, doOnLoad: ICallable): void;

    loadXML(): void;

    loadXMLString(): void;

    removeContextZone(zoneId: any): void;

    removeItem(id: any): void;

    renderAsContextMenu(): void;

    serialize(): any;

    setAlign(align: string): void;

    setAutoHideMode(mode: boolean): void;

    setAutoShowMode(mode: boolean): void;

    setCheckboxState(itemId: string | number, state: boolean): void;

    setContextMenuHideAllMode(mode: boolean): void;

    setHotKey(itemId: any, hkey: string): void;

    setHref(itemId: any, href: string, target: string): void;

    setIconPath(): void;

    setIconsPath(path: string): void;

    setIconset(name: string): void;

    setImagePath(): void;

    setItemDisabled(itemId: any): void;

    setItemEnabled(itemId: any): void;

    setItemImage(itemId: any, img: string, imgDis: string): void;

    setItemPosition(itemId: any, pos: number): void;

    setItemText(itemId: any, text: string): void;

    setOpenMode(mode: string): void;

    setOverflowHeight(itemsNum: number): void;

    setRadioChecked(group: string, itemId: any): void;

    setSkin(skin: string): void;

    setTooltip(itemId: any, tip: string): void;

    setTopText(text: string): void;

    setUserData(itemId: any, name: string, value: string): void;

    setVisibleArea(x1: number, x2: number, y1: number, y2: number): void;

    setWebModeTimeout(tm: number): void;

    showContextMenu(x: number, y: number): void;

    showItem(itemId: any): void;

    unload(): void;

}

type dhtmlXPopupEventName = 'onBeforeHide' | 'onClick' | 'onContentClick' | 'onHide' | 'onShow';

declare class dhtmlXPopup {
    separator: any;

    attachAccordion(width: number, height: number, conf: any): dhtmlXAccordion;

    attachCalendar(): dhtmlXCalendarObject;

    attachCarousel(width: number, height: number, conf: any): dhtmlXCarousel;

    attachColorPicker(conf: any): dhtmlXColorPicker;

    attachEditor(width: number, height: number): dhtmlXEditor;

    attachEvent(name: dhtmlXPopupEventName, handler: ICallable): number;

    attachForm(formData: any): dhtmlXForm;

    attachGrid(width: number, height: number): dhtmlXGridObject;

    attachHTML(html: string): void;

    attachLayout(width: number, height: number, pattern: string): dhtmlXLayoutObject;

    attachList(template: string, data: any[]): void;

    attachObject(obj: any): void;

    attachSidebar(conf: any): dhtmlXSideBar;

    attachTabbar(width: number, height: number, conf: any): dhtmlXTabBar;

    attachTree(width: number, height: number, rootId: string): dhtmlXTreeObject;

    attachTreeView(width: number, height: number, conf: any): dhtmlXTreeViewObject;

    attachVault(width: number, height: number, conf: any): dhtmlXVaultObject;

    clear(): void;

    detachEvent(id: number): void;

    getItemData(id: any): any[];

    hide(): void;

    isVisible(): boolean;

    setDimension(width: number, height: number): void;

    setSkin(skin: string): void;

    show(id: any): void;

    unload(): void;
}

type dhtmlXRibbonEventName =
    'onCheck'
    | 'onClick'
    | 'onEnter'
    | 'onSelect'
    | 'onSelectOption'
    | 'onStateChange'
    | 'onTabClick'
    | 'onTabClose'
    | 'onValueChange'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXRibbon {
    attachEvent(name: dhtmlXRibbonEventName, handler: ICallable): number;

    check(id: any): void;

    detachEvent(id: number): void;

    disable(itemId: string): void;

    enable(itemId: string): void;

    getCombo(id: any): any;

    getInput(id: any): any;

    getItemOptionText(id: any, optId: any): string;

    getItemState(itemId: any, segmId: any): boolean;

    getItemText(itemId: any): string;

    getItemType(itemId: string): string;

    getValue(itemId: string): any;

    hide(itemId: string): void;

    isChecked(id: any): boolean;

    isEnabled(itemId: string): boolean;

    isVisible(itemId: string): boolean;

    loadStruct(data: any, doOnLoad: ICallable): void;

    removeItem(itemId: string): void;

    setIconPath(path: string): void;

    setIconset(name: string): void;

    setItemImage(id: any, img: string): void;

    setItemImageDis(id: any, imgdis: string): void;

    setItemOptionText(item: any, optId: any, text: string): void;

    setItemState(itemId: string, state: boolean): void;

    setItemText(itemId: any, text: string): void;

    setSizes(): void;

    setSkin(skin: string): void;

    setValue(id: string, value: number, callEvent: boolean): void;

    show(itemId: string): void;

    tabs(id: any): dhtmlXCell;

    uncheck(id: any): void;

    unload(): void;

}

type dhtmlXSideBarEventName = 'onBeforeSelect' | 'onBubbleClick' | 'onContentLoaded' | 'onSelect' | 'onXLE' | 'onXLS';

declare class dhtmlXSideBar {
    templates: any;

    addItem(itemConf: any): void;

    attachEvent(name: dhtmlXSideBarEventName, handler: ICallable): number;

    attachFooter(id: any, height: number): void;

    attachHeader(id: any, height: number): void;

    attachMenu(conf: any): dhtmlXMenuObject;

    attachRibbon(conf: any): dhtmlXRibbon;

    attachStatusBar(conf: any): { [key: string]: any; };

    attachToolbar(conf: any): dhtmlXToolbarObject;

    cells(id: any): dhtmlXCell;

    clearAll(): void;

    detachEvent(id: number): void;

    detachFooter(): void;

    detachHeader(): void;

    detachMenu(): void;

    detachRibbon(): void;

    detachStatusBar(): void;

    detachToolbar(): void;

    forEachCell(handler: ICallable): void;

    forEachItem(handler: ICallable): void;

    getActiveItem(): any;

    getAllItems(): any[];

    getAttachedMenu(): dhtmlXMenuObject;

    getAttachedRibbon(): dhtmlXRibbon;

    getAttachedStatusBar(): any;

    getAttachedToolbar(): dhtmlXToolbarObject;

    getNumberOfItems(): number;

    goToNextItem(callEvent: boolean): void;

    goToPrevItem(callEvent: boolean): void;

    hideMenu(): void;

    hideRibbon(): void;

    hideSide(): void;

    hideStatusBar(): void;

    hideToolbar(): void;

    items(id: any): dhtmlXCell;

    loadStruct(data: any, doOnLoad: ICallable): void;

    progressOff(): void;

    progressOn(): void;

    removeSep(id: any): void;

    setOffsets(conf: any): void;

    setSideWidth(width: number): void;

    setSizes(): void;

    setTemplate(template: string, iconsPath: string): void;

    showMenu(): void;

    showRibbon(): void;

    showSide(): void;

    showStatusBar(): void;

    showToolbar(): void;

    unload(): void;
}

type dhtmlXSliderEventName = 'onChange' | 'onMouseDown' | 'onMouseUp' | 'onSlideEnd';

declare class dhtmlXSlider {
    attachEvent(name: dhtmlXSliderEventName, handler: ICallable): number;

    detachEvent(id: number): void;

    disable(): void;

    disableTooltip(): void;

    enable(): void;

    enableTooltip(): void;

    getMax(): number;

    getMin(): number;

    getRunnerIndex(): number;

    getStep(): number;

    getValue(): any;

    hide(): void;

    init(): void;

    isEnabled(): boolean;

    isVisible(): boolean;

    linkTo(obj: any): void;

    setImagePath(): void;

    setMax(val: number): void;

    setMin(val: number): void;

    setOnChangeHandler(): void;

    setSize(value: number): void;

    setSkin(skin: string): void;

    setStep(val: number): void;

    setValue(value: number | any[], callEvent: boolean): void;

    show(): void;

    unload(): void;

}

type dhtmlXTabBarEventName =
    'onContentLoaded'
    | 'onSelect'
    | 'onTabClick'
    | 'onTabClose'
    | 'onTabContentLoaded'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXTabBar {
    addTab(id: string | number, text: string, width: number, position: number, active: boolean, close: boolean): void;

    adjustOuterSize(): void;

    attachEvent(name: dhtmlXTabBarEventName, handler: ICallable): number;

    attachFooter(id: any, height: number): void;

    attachHeader(id: any, height: number): void;

    attachMenu(conf: any): dhtmlXMenuObject;

    attachRibbon(conf: any): dhtmlXRibbon;

    attachStatusBar(conf: any): { [key: string]: any; };

    attachToolbar(conf: any): dhtmlXToolbarObject;

    cells(id: any): dhtmlXCell;

    clearAll(): void;

    destructor(): void;

    detachEvent(id: number): void;

    detachFooter(): void;

    detachHeader(): void;

    detachMenu(): void;

    detachRibbon(): void;

    detachStatusBar(): void;

    detachToolbar(): void;

    disableTab(): void;

    enableAutoReSize(): void;

    enableAutoSize(): void;

    enableContentZone(mode: boolean): void;

    enableForceHiding(): void;

    enableScroll(): void;

    enableTab(): void;

    enableTabCloseButton(mode: boolean): void;

    forEachCell(handler: ICallable): void;

    forEachTab(handler: ICallable): void;

    forceLoad(id: any): void;

    getActiveTab(): any;

    getAllTabs(): any[];

    getAttachedMenu(): dhtmlXMenuObject;

    getAttachedRibbon(): dhtmlXRibbon;

    getAttachedStatusBar(): any;

    getAttachedToolbar(): dhtmlXToolbarObject;

    getIndex(): number;

    getLabel(): string;

    getNumberOfTabs(): number;

    goToNextTab(): void;

    goToPrevTab(): void;

    hideMenu(): void;

    hideRibbon(): void;

    hideStatusBar(): void;

    hideTab(): void;

    hideToolbar(): void;

    loadStruct(data: any, doOnLoad: ICallable): void;

    loadXML(xmlUrl: string, doOnLoad: ICallable): void;

    loadXMLString(xmlString: string, doOnLoad: ICallable): void;

    moveTab(id: any, index: number): void;

    normalize(): void;

    progressOff(): void;

    progressOn(): void;

    removeTab(): void;

    setAlign(align: string): void;

    setArrowsMode(mode: string): void;

    setContent(id: any, obj: any): void;

    setContentHTML(id: any, htmlString: string): void;

    setContentHref(id: any, href: string): void;

    setCustomStyle(): void;

    setHrefMode(mode: string): void;

    setImagePath(): void;

    setLabel(): void;

    setMargin(): void;

    setOffset(): void;

    setOffsets(conf: any): void;

    setSize(): void;

    setSizes(): void;

    setSkin(skin: string): void;

    setSkinColors(): void;

    setStyle(): void;

    setTabActive(): void;

    setTabInActive(): void;

    setTabsMode(mode: string): void;

    showInnerScroll(): void;

    showMenu(): void;

    showRibbon(): void;

    showStatusBar(): void;

    showTab(): void;

    showToolbar(): void;

    tabWindow(id: any): void;

    tabs(id: any): dhtmlXCell;

    unload(): void;

}

type dhtmlXToolbarObjectEventName =
    'onBeforeStateChange'
    | 'onButtonSelectHide'
    | 'onButtonSelectShow'
    | 'onClick'
    | 'onEnter'
    | 'onStateChange'
    | 'onValueChange'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXToolbarObject {
    addButton(id: any, pos: number, text: string, imgEn: string, imgDis: string): void;

    addButtonSelect(id: string, pos: number, text: string, opts: any[], imgEn: string, imgDis: string, renderSelect: boolean, openAll: boolean, maxOpen: number, mode: string): void;

    addButtonTwoState(id: any, pos: number, text: string, imgEn: string, imgDis: string): void;

    addInput(id: any, pos: number, value: string, width: number): void;

    addListOption(parentId: any, optionId: any, pos: number, type: string, text: string, img: string): void;

    addSeparator(id: any, pos: number): void;

    addSlider(id: any, pos: number, len: number, valueMin: number, valueMax: number, valueNow: number, textMin: string, textMax: string, tip: string): void;

    addSpacer(itemId: any): void;

    addText(id: any, pos: number, text: string): void;

    attachEvent(name: dhtmlXToolbarObjectEventName, handler: ICallable): number;

    clearAll(): void;

    clearItemImage(itemId: any): void;

    clearItemImageDis(itemId: any): void;

    clearListOptionImage(parentId: any, optionId: any): void;

    detachEvent(id: number): void;

    disableItem(itemId: any): void;

    disableListOption(parentId: any, optionId: any): void;

    enableItem(itemId: any): void;

    enableListOption(parentId: any, optionId: any): void;

    forEachItem(handler: ICallable): void;

    forEachListOption(parentId: any, handler: ICallable): void;

    getAllListOptions(parentId: any): any;

    getInput(id: any): any;

    getItemState(itemId: any): boolean;

    getItemText(itemId: any): string;

    getItemToolTip(itemId: any): string;

    getItemToolTipTemplate(itemId: any): string;

    getListOptionImage(parentId: any, optionId: any): string;

    getListOptionPosition(parentId: any, optionId: any): number;

    getListOptionSelected(parentId: any): any;

    getListOptionText(parentId: any, optionId: any): string;

    getListOptionToolTip(parentId: any, optionId: any): string;

    getListOptionUserData(parentId: any, optionId: any, name: string): any;

    getMaxValue(itemId: any): any;

    getMinValue(itemId: any): any;

    getParentId(optionId: any): any;

    getPosition(itemId: any): number;

    getType(itemId: string): string;

    getTypeExt(itemId: any): any;

    getUserData(itemId: any, name: any): any;

    getValue(itemId: any): any;

    getWidth(itemId: any): number;

    hideItem(itemId: any): void;

    hideListOption(parentId: any, optionId: any): void;

    isEnabled(itemId: any): boolean;

    isListOptionEnabled(parentId: any, optionId: any): boolean;

    isListOptionVisible(parentId: any, optionId: any): boolean;

    isVisible(itemId: any): boolean;

    loadStruct(data: any, doOnLoad: ICallable): void;

    loadXML(): void;

    loadXMLString(): void;

    removeItem(itemId: any): void;

    removeListOption(parentId: any, optionId: any): void;

    removeSpacer(itemId: any): void;

    setAlign(mode: string): void;

    setIconPath(): void;

    setIconSize(size: number): void;

    setIconsPath(path: string): void;

    setIconset(name: string): void;

    setItemImage(itemId: any, url: string): void;

    setItemImageDis(itemId: any, url: string): void;

    setItemState(itemId: string | number, state: boolean): void;

    setItemText(itemId: any, text: string): void;

    setItemToolTip(itemId: any, tip: string): void;

    setItemToolTipTemplate(itemId: any, template: string): void;

    setListOptionImage(parentId: any, optionId: any, img: string): void;

    setListOptionPosition(parentId: any, optionId: any, pos: number): void;

    setListOptionSelected(parentId: any, optionId: any): void;

    setListOptionText(parentId: any, optionId: any, text: string): void;

    setListOptionToolTip(parentId: any, optionId: any, tip: string): void;

    setListOptionUserData(parentId: any, optionId: any, name: string, value: any): void;

    setMaxOpen(itemId: any, max: number): void;

    setMaxValue(itemId: any, value: number, label: string): void;

    setMinValue(itemId: any, value: number, label: string): void;

    setPosition(itemId: any, pos: number): void;

    setSkin(name: string): void;

    setUserData(itemId: any, name: string, value: any): void;

    setValue(itemId: string | number, value: number, CallEvent: boolean): void;

    setWidth(itemId: any, width: number): void;

    showItem(itemId: any): void;

    showListOption(parentId: any, optionId: any): void;

    unload(): void;

}

type dhtmlXTreeObjectEventName =
    'onAllOpenDynamic'
    | 'onBeforeCheck'
    | 'onBeforeContextMenu'
    | 'onBeforeDrag'
    | 'onCheck'
    | 'onClick'
    | 'onDblClick'
    | 'onDrag'
    | 'onDragIn'
    | 'onDrop'
    | 'onEdit'
    | 'onEditCancel'
    | 'onKeyPress'
    | 'onMouseIn'
    | 'onMouseOut'
    | 'onOpenDynamicEnd'
    | 'onOpenEnd'
    | 'onOpenStart'
    | 'onRightClick'
    | 'onSelect'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXTreeObject {
    assignKeys(keys: any[]): void;

    attachEvent(name: dhtmlXTreeObjectEventName, handler: ICallable): number;

    changeItemId(oldId: any, newId: any): void;

    clearCut(): void;

    clearSelection(id: any): void;

    closeAllItems(id: any): void;

    closeItem(id: any): void;

    deleteChildItems(id: any): void;

    deleteItem(id: string | number, selectParent: boolean): void;

    destructor(): void;

    detachEvent(id: number): void;

    disableCheckbox(id: string | number, mode: boolean): void;

    doCut(): void;

    doPaste(id: any): void;

    editItem(id: any): void;

    enableActiveImages(mode: boolean): void;

    enableAutoSavingSelected(mode: boolean): void;

    enableAutoTooltips(mode: boolean): void;

    enableCheckBoxes(mode: boolean, hidden: boolean): void;

    enableContextMenu(menu: dhtmlXMenuObject): void;

    enableDistributedParsing(mode: boolean, count: number, delay: number): void;

    enableDragAndDrop(mode: any, rmode: boolean): void;

    enableDragAndDropScrolling(mode: boolean): void;

    enableHighlighting(mode: boolean): void;

    enableIEImageFix(mode: boolean): void;

    enableImageDrag(mode: boolean): void;

    enableItemEditor(mode: boolean): void;

    enableKeySearch(mode: boolean): void;

    enableKeyboardNavigation(mode: boolean): void;

    enableLoadingItem(text: string): void;

    enableMercyDrag(mode: boolean): void;

    enableMultiLineItems(width: number): void;

    enableMultiselection(mode: boolean, strict: boolean): void;

    enableRTL(mode: boolean): void;

    enableRadioButtons(mode: boolean, id: string | number): void;

    enableSingleRadioMode(mode: boolean, id: string | number): void;

    enableSmartCheckboxes(mode: boolean): void;

    enableSmartRendering(): void;

    enableSmartXMLParsing(mode: boolean): void;

    enableTextSigns(mode: boolean): void;

    enableThreeStateCheckboxes(mode: boolean): void;

    enableTreeImages(mode: boolean): void;

    enableTreeLines(mode: boolean): void;

    findItem(searchStr: string, direction: number, top: number): void;

    findItemIdByLabel(searchStr: string, direction: number, top: number): void;

    getAllChecked(): any[];

    getAllCheckedBranches(): any[];

    getAllChildless(): any[];

    getAllItemsWithKids(): any[];

    getAllPartiallyChecked(): any[];

    getAllSubItems(id: any): any[];

    getAllUnchecked(): any[];

    getAttribute(id: any, name: string): any;

    getChildItemIdByIndex(id: any, index: number): any;

    getDistributedParsingState(): boolean;

    getIndexById(id: any): number;

    getItemColor(id: any): string;

    getItemIdByIndex(id: any, index: number): any;

    getItemImage(id: any, imageInd: number, value: number): string;

    getItemParsingState(): number;

    getItemText(id: any): string;

    getItemTooltip(id: any): string;

    getLevel(id: any): number;

    getOpenState(id: any): boolean;

    getParentId(id: any): any;

    getSelectedItemId(): any;

    getSelectedItemText(): string;

    getSubItems(id: any): any[];

    getUserData(id: any, name: string): any;

    getXMLState(): boolean;

    hasChildren(id: any): number;

    insertNewChild(parentId: any, id: any, text: string, actionHandler: ICallable, image1: string, image2: string, image3: string, optionStr: string, children: any): void;

    insertNewItem(parentId: any, id: any, text: string, actionHandler: ICallable, image1: string, image2: string, image3: string, optionStr: string, children: any): void;

    insertNewNext(nextToId: any, id: any, text: string, actionHandler: ICallable, image1: string, image2: string, image3: string, optionStr: string, children: any): void;

    isItemChecked(id: any): boolean;

    isLocked(id: any): boolean;

    load(url: string, call: ICallable, type: string): void;

    loadCSV(csvFile: string, afterCall: ICallable, type: string): void;

    loadCSVString(csvString: string, afterCall: ICallable, type: string): void;

    loadJSArray(JSArray: any[], afterCall: ICallable, type: string): void;

    loadJSArrayFile(jsarrayFile: string, afterCall: ICallable, type: string): void;

    loadJSON(jsonFile: string, afterCall: ICallable, type: string): void;

    loadJSONObject(jsonObject: any, afterCall: ICallable, type: string): void;

    loadOpenStates(name: string): void;

    loadState(name: string): void;

    loadXML(xmlFile: string, afterCall: ICallable, type: string): void;

    loadXMLString(xmlString: string, afterCall: ICallable, type: string): void;

    lockItem(id: string | number, mode: boolean): void;

    lockTree(mode: boolean): void;

    makeAllDraggable(func: ICallable): void;

    makeDraggable(obj: any, func: ICallable): void;

    moveItem(id: any, mode: string, targetId: any, targetTree: dhtmlXTreeObject): void;

    openAllItems(id: any): void;

    openAllItemsDynamic(id: any): void;

    openItem(id: any): void;

    openItemsDynamic(list: string, flag: boolean): void;

    openOnItemAdded(mode: boolean): void;

    parse(data: string | { [key: string]: any; }, type: string): void;

    preventIECaching(mode: boolean): void;

    refreshItem(id: any): void;

    refreshItems(itemIdList: string, source: string): void;

    registerXMLEntity(rChar: string, rEntity: any): void;

    restoreSelectedItem(name: string): void;

    saveOpenStates(name: string, cookieParam: any): void;

    saveSelectedItem(name: string, cookieParam: any): void;

    saveState(name: string, cookieParam: any): void;

    selectItem(id: string | number, mode: boolean, preserve: boolean): void;

    serializeTree(): string;

    serializeTreeToJSON(): string;

    setAttribute(id: any, name: string, value: any): void;

    setCheck(id: any, state: any): void;

    setChildCalcHTML(htmlA: string, htmlB: string): void;

    setChildCalcMode(mode: string): void;

    setCustomSortFunction(func: ICallable): void;

    setDataMode(mode: string): void;

    setDragBehavior(mode: string, select: boolean): void;

    setEditStartAction(click: boolean, dblclick: boolean): void;

    setEscapingMode(mode: string): void;

    setIconSize(newWidth: string, newHeight: string, id: any): void;

    setIconsPath(path: string): void;

    setImageArrays(arrayName: string, image1: string, image2: string, image3: string, image4: string, image5: string): void;

    setImagesPath(path: string): void;

    setItemCloseable(id: string | number, flag: boolean): void;

    setItemColor(id: any, defaultColor: string, selectedColor: string): void;

    setItemContextMenu(id: any, menu: dhtmlXMenuObject): void;

    setItemImage(id: any, im1: string, im2: string): void;

    setItemStyle(id: any, styleString: string): void;

    setItemText(id: any, newLabel: string, newTooltip: string): void;

    setItemTopOffset(id: any, value: number): void;

    setListDelimeter(separator: string): void;

    setLockedIcons(im0: string, im1: string, im2: string): void;

    setSerializationLevel(userData: boolean, fullXML: boolean, escapeEntities: boolean, userDataAsCData: boolean, DTD: boolean): void;

    setSkin(skin: string): void;

    setStdImages(im0: string, im1: string, im2: string): void;

    setSubChecked(id: string | number, state: boolean): void;

    setUserData(id: any, name: string, value: any): void;

    setXMLAutoLoading(filePath: string): void;

    setXMLAutoLoadingBehaviour(mode: any): void;

    showItemCheckbox(id: string | number, state: boolean): void;

    showItemSign(id: string | number, state: boolean): void;

    smartRefreshBranch(id: any, source: string): void;

    smartRefreshItem(id: any, source: string): void;

    sortTree(nodeId: string | number, order: string, allLevels: boolean): void;

    stopEdit(): void;

    updateItem(itemId: any[], name: any[], im0: any[], im1: any[], im2: any[], checked: any[]): void;

}

type dhtmlXTreeGridEventName =
    'onAfterCMove'
    | 'onAfterRowDeleted'
    | 'onAfterSorting'
    | 'onBeforeBlockSelected'
    | 'onBeforeCMove'
    | 'onBeforeContextMenu'
    | 'onBeforeDrag'
    | 'onBeforeFormSubmit'
    | 'onBeforePageChanged'
    | 'onBeforeRowDeleted'
    | 'onBeforeSelect'
    | 'onBeforeSorting'
    | 'onBlockRightClick'
    | 'onBlockSelected'
    | 'onCalendarShow'
    | 'onCellChanged'
    | 'onCellMarked'
    | 'onCellUnMarked'
    | 'onCheck'
    | 'onCheckbox'
    | 'onClearAll'
    | 'onCollectValues'
    | 'onColumnCollapse'
    | 'onColumnHidden'
    | 'onDataReady'
    | 'onDhxCalendarCreated'
    | 'onDistributedEnd'
    | 'onDrag'
    | 'onDragIn'
    | 'onDragOut'
    | 'onDrop'
    | 'onDynXLS'
    | 'onEditCancel'
    | 'onEditCell'
    | 'onEmptyClick'
    | 'onEnter'
    | 'onFilterEnd'
    | 'onFilterStart'
    | 'onGridReconstructed'
    | 'onGroup'
    | 'onGroupClick'
    | 'onGroupStateChanged'
    | 'onHeaderClick'
    | 'onKeyPress'
    | 'onLastRow'
    | 'onLiveValidationCorrect'
    | 'onLiveValidationError'
    | 'onMouseOver'
    | 'onOpenEnd'
    | 'onOpenStart'
    | 'onPageChanged'
    | 'onPaging'
    | 'onResize'
    | 'onResizeEnd'
    | 'onRightClick'
    | 'onRowAdded'
    | 'onRowCreated'
    | 'onRowDblClicked'
    | 'onRowHide'
    | 'onRowIdChange'
    | 'onRowInserted'
    | 'onRowPaste'
    | 'onRowSelect'
    | 'onScroll'
    | 'onSelectStateChanged'
    | 'onStatReady'
    | 'onSubAjaxLoad'
    | 'onSubGridCreated'
    | 'onSubRowOpen'
    | 'onSyncApply'
    | 'onTab'
    | 'onUndo'
    | 'onUnGroup'
    | 'onValidationCorrect'
    | 'onValidationError'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXTreeGrid {
    csvParser: any;
    editor: any;
    kidsXmlFile: string;

    addRow(new_id: string | number, text: any[], ind: number, parent_id: string | number, img: string, child: boolean): void;

    addRowAfter(new_id: string | number, text: any[], sibl_id: string | number, img: string, child: boolean): void;

    addRowBefore(new_id: string | number, text: any[], sibl_id: string | number, img: string, child: boolean): void;

    addRowFromClipboard(): void;

    adjustColumnSize(cInd: number): void;

    attachEvent(evName: dhtmlXTreeGridEventName, evHandler: ICallable): void;

    attachFooter(values: any[], style: any[]): void;

    attachHeader(values: any[], style?: any[]): void;

    attachToObject(obj: any): void;

    cellById(row_id: string | number, col_ind: number): void;

    cellByIndex(row_ind: number, col_ind: number): void;

    cellToClipboard(rowId: string | number, cellInd: number): void;

    cells(row_id: string | number, col: number): void;

    cells2(row_index: number, col: number): void;

    changePage(pageNum: number): void;

    changePageRelative(ind: number): void;

    changeRowId(oldRowId: string | number, newRowId: string | number): void;

    checkAll(mode: boolean): void;

    clearAll(header: boolean): void;

    clearAndLoad(url: string, call: ICallable, type: string): void;

    clearChangedState(): void;

    clearConfigCookie(name: string): void;

    clearSelection(): void;

    closeItem(rowId: string | number): void;

    collapseAll(): void;

    collapseAllGroups(): void;

    collapseColumns(cInd: number): void;

    collapseGroup(val: string): void;

    collectTreeValues(column: number): any[];

    collectValues(column: number): any[];

    copyBlockToClipboard(): void;

    copyRowContent(from_row: string | number, to_row_id: string | number): void;

    deleteChildItems(rowId: string | number): void;

    deleteColumn(ind: number): void;

    deleteRow(row_id: string | number): void;

    deleteSelectedRows(): void;

    destructor(): void;

    detachEvent(id: string): void;

    detachFooter(index: number): void;

    detachHeader(index: number): void;

    disableUndoRedo(): void;

    doRedo(): void;

    doUndo(): void;

    doesRowExist(row_id: string | number): void;

    editCell(): void;

    editStop(ode: boolean): void;

    enableAccessKeyMap(): void;

    enableAlterCss(cssE: string, cssU: string, perLevel: boolean, levelUnique: boolean): void;

    enableAutoHeight(mode: boolean, maxHeight: number, countFullHeight: boolean): void;

    enableAutoHiddenColumnsSaving(name: string, cookie_param: string): void;

    enableAutoSaving(name: string, cookie_param: string): void;

    enableAutoSizeSaving(name: string, cookie_param: string): void;

    enableAutoWidth(mode: boolean, max_limit: number, min_limit: number): void;

    enableBlockSelection(mode: boolean): void;

    enableCSVAutoID(mode: boolean): void;

    enableCSVHeader(mode: boolean): void;

    enableCellIds(mode: boolean): void;

    enableColSpan(mode: boolean): void;

    enableColumnAutoSize(mode: boolean): void;

    enableColumnMove(mode: boolean, columns: string): void;

    enableContextMenu(menu: any): void;

    enableDistributedParsing(mode: boolean, count: number, time: number): void;

    enableDragAndDrop(mode: boolean): void;

    enableDragOrder(mode: any): void;

    enableEditEvents(click: boolean, dblclick: boolean, f2Key: boolean): void;

    enableEditTabOnly(state: boolean): void;

    enableExcelKeyMap(): void;

    enableHeaderImages(mode: boolean): void;

    enableHeaderMenu(list: string): void;

    enableKeyboardSupport(mode: boolean): void;

    enableLightMouseNavigation(mode: boolean): void;

    enableMarkedCells(mode: boolean): void;

    enableMathEditing(mode: boolean): void;

    enableMathSerialization(mode: boolean): void;

    enableMercyDrag(mode: boolean): void;

    enableMultiline(state: boolean): void;

    enableMultiselect(state: boolean): void;

    enableOrderSaving(name: string, cookie_param: string): void;

    enablePaging(mode: boolean, pageSize: number, pagesInGrp: number, pagingControlsContainer: number | HTMLElement, showRecInfo: boolean, pagingStateContainer: number | HTMLElement): void;

    enablePreRendering(buffer: number): void;

    enableResizing(list: string): void;

    enableRowsHover(mode: boolean, cssClass: string): void;

    enableRowspan(): void;

    enableSmartRendering(mode: boolean, buffer: number): void;

    enableSmartXMLParsing(mode: boolean): void;

    enableSortingSaving(name: string, cookie_param: string): void;

    enableStableSorting(mode: boolean): void;

    enableTooltips(list: string): void;

    enableTreeCellEdit(mode: boolean): void;

    enableTreeGridLines(mode: boolean): void;

    enableUndoRedo(): void;

    enableValidation(mode: boolean): void;

    expandAll(): void;

    expandAllGroups(): void;

    expandColumns(cInd: number): void;

    expandGroup(val: string): void;

    filterBy(column: number, value: string, preserve: boolean): void;

    filterByAll(): void;

    filterTreeBy(column: number, value: string, preserve: boolean): void;

    findCell(value: string, c_ind: number, first: boolean): void;

    forEachCell(rowId: any, custom_code: ICallable): void;

    forEachRow(custom_code: ICallable): void;

    forEachRowInGroup(name: string, custom_code: ICallable): void;

    forceFullLoading(buffer: number): void;

    forceLabelSelection(mode: boolean): void;

    getAllRowIds(separator: string): string;

    getAllSubItems(rowId: string | number): any[];

    getChangedRows(nd_added: boolean): string;

    getCheckedRows(col_ind: number): string;

    getChildItemIdByIndex(rowId: string | number, ind: number): string | number;

    getColIndexById(id: number): number;

    getColLabel(cin: number, ind: number): string;

    getColType(cInd: number): string;

    getColTypeById(cID: any): string;

    getColWidth(ind: number): number;

    getColumnCombo(column_index: number): any;

    getColumnId(cin: number): any;

    getColumnLabel(cin: number, ind: number): string;

    getColumnsNum(): number;

    getCombo(col_ind: number): any;

    getCustomCombo(id: any, ind: number): any;

    getFilterElement(index: number): any;

    getFooterLabel(cin: number, ind: number, mode: boolean): string;

    getHeaderMenu(columns: any): any;

    getItemIcon(rowId: string | number): string;

    getItemImage(rowId: string | number): string;

    getItemText(rowId: string | number): string;

    getLevel(rowId: string | number): number;

    getMarked(): any[];

    getOpenState(rowId: string | number): boolean;

    getParentId(rowId: string | number): string | number;

    getRedo(): any[];

    getRowAttribute(rId: any, name: string): any;

    getRowId(ind: number): any;

    getRowIndex(row_id: any): number;

    getRowsNum(): number;

    getSelectedBlock(): any;

    getSelectedCellIndex(): number;

    getSelectedRowId(): any;

    getSortingState(): string;

    getStateOfView(): any[];

    getSubItems(rowId: string | number): string;

    getUndo(): any[];

    getUserData(row_id: any, name: any): any;

    gridFromClipboard(): void;

    gridToClipboard(): void;

    gridToGrid(rowId: any, sgrid: any, tgrid: any): void;

    gridToTreeElement(treeObj: any, treeNodeId: any, gridRowId: any): void;

    groupBy(ind: number, mask: any[]): void;

    groupStat(key: string, ind: number, item: string): number;

    hasChildren(rowId: string | number): number;

    init(): void;

    insertColumn(ind: number, header: string, type: string, width: number, sort: string, align: string, valign: string, reserved: any, columnColor: string): void;

    isColumnHidden(ind: number): void;

    load(url: string, call: ICallable, type: string): void;

    loadHiddenColumnsFromCookie(name: string): void;

    loadOpenStates(name: string): void;

    loadOrderFromCookie(name: string): void;

    loadSizeFromCookie(name: string): void;

    loadSortingFromCookie(name: string): void;

    lockRow(rowId: any, mode: boolean): void;

    makeFilter(id: number | HTMLElement, column: number, preserve: boolean): void;

    makeSearch(id: any, column: number): void;

    mark(row: string | number, cInd: number, state: boolean): void;

    moveColumn(oldInd: number, newInd: number): void;

    moveRow(rowId: any, mode: string, targetId: any, targetGrid: any): void;

    moveRowDown(row_id: any): void;

    moveRowTo(srowId: any, trowId: any, mode: string, dropmode: string, sourceGrid: any, targetGrid: any): void;

    moveRowUp(row_id: any): void;

    openItem(rowId: string | number): void;

    parse(data: string | { [key: string]: any; }, type: string): void;

    pasteBlockFromClipboard(): void;

    post(url: string, post: string, call: ICallable, type: string): void;

    preventIECaching(mode: boolean): void;

    printView(before: string, after: string): void;

    refreshComboColumn(index: number): void;

    refreshFilters(): void;

    refreshMath(): void;

    registerCList(col: number, list: any[]): void;

    rowToClipboard(rowId: any): void;

    rowToDragElement(id: any): void;

    saveHiddenColumnsToCookie(name: string, cookie_param: string): void;

    saveOpenStates(name: string): void;

    saveOrderToCookie(name: string, cookie_param: string): void;

    saveSizeToCookie(name: string, cookie_param: string): void;

    saveSortingToCookie(name: string, cookie_param: string): void;

    selectAll(): void;

    selectBlock(start_row: string | number, start_col: number, end_row: string | number, end_column: number): void;

    selectCell(row: number | HTMLElement, cInd: number, preserve: boolean, edit: boolean, show: boolean): void;

    selectRow(row: number | HTMLElement, fl: boolean, preserve: boolean, show: boolean): void;

    selectRowById(row_id: string | number, preserve: boolean, show: boolean, call: boolean): void;

    serialize(): void;

    serializeToCSV(text_only: boolean): void;

    setActive(mode: boolean): void;

    setAwaitedRowHeight(height: number): void;

    setCSVDelimiter(str: string): void;

    setCellExcellType(rowId: any, cellIndex: number, type: string): void;

    setCellTextStyle(row_id: any, ind: number, styleString: string): void;

    setCheckedRows(col_ind: number, v: number): void;

    setColAlign(alStr: string): void;

    setColLabel(col: number, ind: number): void;

    setColSorting(sortStr: string): void;

    setColTypes(typeStr: string): void;

    setColVAlign(valStr: string): void;

    setColValidators(vals: string): void;

    setColWidth(ind: number, value: string): void;

    setColspan(row_id: string | number, col_index: number, colspan: number): void;

    setColumnColor(clr: string): void;

    setColumnExcellType(colIndex: number, type: string): void;

    setColumnHidden(ind: number, state: boolean): void;

    setColumnId(ind: number, id: any): void;

    setColumnIds(ids: string): void;

    setColumnLabel(col: number, ind: number): void;

    setColumnMinWidth(width: number, ind: number): void;

    setColumnsVisibility(list: string): void;

    setCustomSorting(func: ICallable, col: number): void;

    setDateFormat(mask: string, server_mask: string): void;

    setDelimiter(delim: string): void;

    setDragBehavior(mode: string): void;

    setEditable(mode: boolean): void;

    setExternalTabOrder(start: any, end: any): void;

    setFieldName(name: string): void;

    setFiltrationLevel(level: number, show_upper: boolean): void;

    setFooterLabel(col: number, label: string, ind: number): void;

    setHeader(hdrStr: string, splitSign?: string, styles?: any[]): void;

    setIconsPath(path: string): void;

    setIconset(name: string): void;

    setImageSize(width: number, height: number): void;

    setImagesPath(path: string): void;

    setInitWidths(wp: string): void;

    setInitWidthsP(wp: string): void;

    setItemCloseable(rowId: string | number, status: boolean): void;

    setItemIcon(rowId: string | number, icon: string): void;

    setItemImage(rowId: string | number, url: string): void;

    setItemText(rowId: string | number, newtext: string): void;

    setMathRound(digits: number): void;

    setNoHeader(fl: boolean): void;

    setNumberFormat(mask: string, cInd: number, p_sep: string, d_sep: string): void;

    setOnOpenEndHandler(func: ICallable): void;

    setOnOpenStartHandler(func: ICallable): void;

    setPagingSkin(name: string): void;

    setPagingTemplates(navigation_template: string, info_template: string): void;

    setPagingWTMode(navButtons: boolean, navLabel: boolean, pageSelect: boolean, perPageSelect: boolean | any[]): void;

    setRowAttribute(id: any, name: string, value: any): void;

    setRowColor(row_id: any, color: string): void;

    setRowExcellType(rowId: any, type: string): void;

    setRowHidden(id: string | number, state: boolean): void;

    setRowId(ind: number, row_id: any): void;

    setRowTextBold(row_id: any): void;

    setRowTextNormal(row_id: any): void;

    setRowTextStyle(row_id: any, styleString: string): void;

    setRowspan(rowID: any, colInd: number, length: number): void;

    setSerializableColumns(list: string): void;

    setSerializationLevel(userData: boolean, selectedAttr: boolean, config: boolean, changedAttr: boolean, onlyChanged: boolean, asCDATA: boolean): void;

    setSizes(): void;

    setSkin(name: string): void;

    setSortImgState(state: boolean, ind: number, order: string, row: number): void;

    setStyle(ss_header: string, ss_grid: string, ss_selCell: string, ss_selRow: string): void;

    setSubGrid(subgrid: any, sInd: number, tInd: number): void;

    setSubTree(subgrid: any, sInd: number): void;

    setTabOrder(order: string): void;

    setUserData(row_id: any, name: string, value: any): void;

    showRow(rowID: any): void;

    sortRows(col: number, type: string, order: string): void;

    sortTreeRows(col: number, type: string, order: string): void;

    splitAt(ind: number): void;

    startFastOperations(): void;

    stopFastOperations(): void;

    submitAddedRows(mode: boolean): void;

    submitColumns(inds: string): void;

    submitOnlyChanged(mode: boolean): void;

    submitOnlyRowID(mode: boolean): void;

    submitOnlySelected(mode: boolean): void;

    submitSerialization(mode: boolean): void;

    toExcel(path: string): void;

    toPDF(path: any): void;

    treeToGridElement(treeObj: any, treeNodeId: any, gridRowId: any): void;

    uid(): void;

    unGroup(): void;

    uncheckAll(): void;

    unmarkAll(): void;

    updateCellFromClipboard(rowId: any, cellInd: number): void;

    updateFromXML(url: string, insert_new: boolean, del_missed: boolean, afterCall: ICallable): void;

    updateGroups(): void;

    updateRowFromClipboard(rowId: any): void;

    validateCell(id: any, index: number, rule: ICallable): void;
}

type dhtmlXTreeViewObjectEventName =
    'onAddItem'
    | 'onBeforeCheck'
    | 'onBeforeDeleteItem'
    | 'onBeforeDrag'
    | 'onBeforeDrop'
    | 'onCheck'
    | 'onContextMenu'
    | 'onDeleteItem'
    | 'onDragOver'
    | 'onDrop'
    | 'onSelect'
    | 'onTextChange'
    | 'onXLE'
    | 'onXLS';

declare class dhtmlXTreeViewObject {
    addItem(id: string | number, text: string, parentId: string | number, index: number): void;

    attachEvent(name: dhtmlXTreeViewObjectEventName, handler: ICallable): number;

    checkItem(id: string | number): void;

    clearAll(): void;

    closeItem(id: string | number): void;

    deleteItem(id: string | number): void;

    detachEvent(id: number): void;

    disableCheckbox(id: string | number): void;

    enableCheckbox(id: string | number): void;

    enableCheckboxes(mode: boolean): void;

    enableContextMenu(mode: boolean): void;

    enableDragAndDrop(mode: boolean): void;

    enableMultiselect(mode: boolean): void;

    getAllChecked(): any[];

    getAllUnchecked(): any[];

    getItemText(id: string | number): string;

    getParentId(id: string | number): string | number;

    getSelectedId(): string | number;

    getSubItems(id: string | number): any[];

    getUserData(id: string | number, name: string): string | number | boolean;

    hideCheckbox(id: string | number): void;

    isCheckboxEnabled(id: string | number): boolean;

    isCheckboxVisible(id: string | number): boolean;

    isItemChecked(id: string | number): boolean;

    loadStruct(data: any, doOnLoad: ICallable): void;

    openItem(id: string): void;

    selectItem(id: string | number): void;

    setIconColor(id: string | number, color: string): void;

    setIconset(name: string): void;

    setItemIcons(id: string | number, icons: any): void;

    setItemText(id: string | number, text: string): void;

    setSizes(): void;

    setSkin(skin: string): void;

    setUserData(id: any, name: string, value: string): void;

    showCheckbox(id: string | number): void;

    silent(callback: ICallable): void;

    uncheckItem(id: string | number): void;

    unload(): void;

    unselectItem(id: string | number): void;

}

type dhtmlXVaultObjectEventName =
    'onBeforeClear'
    | 'onBeforeFileAdd'
    | 'onBeforeFileRemove'
    | 'onClear'
    | 'onDrop'
    | 'onFileAdd'
    | 'onFileRemove'
    | 'onUploadCancel'
    | 'onUploadComplete'
    | 'onUploadFail'
    | 'onUploadFile';

declare class dhtmlXVaultObject {
    icons: boolean;
    strings: boolean;

    addDraggableNode(nodeId: any, fileData: any): void;

    addFileRecord(fileData: any, status: string): void;

    attachEvent(name: dhtmlXVaultObjectEventName, handler: ICallable): number;

    clear(): void;

    create(): void;

    detachEvent(id: number): void;

    disable(): void;

    enable(): void;

    getData(): any;

    getFileExtension(fileName: string): string;

    getMaxFileSize(): number;

    getSLVersion(): void;

    getStatus(): number;

    load(data: any, doOnLoad: ICallable): void;

    onAddFile(): void;

    onFileUploaded(): void;

    onUploadComplete(): void;

    readableSize(size: number): string;

    removeDraggableNode(nodeId: any): void;

    setAutoRemove(mode: boolean): void;

    setAutoStart(mode: boolean): void;

    setDownloadURL(url: string): void;

    setFilesLimit(count: number): void;

    setFormField(): void;

    setHeight(height: number): void;

    setImagePath(): void;

    setMaxFileSize(size: number): void;

    setProgressMode(mode: string): void;

    setSLURL(slUrl: string): void;

    setSWFURL(swfUrl: string): void;

    setServerHandlers(): void;

    setSizes(): void;

    setSkin(skin: string): void;

    setStrings(data: any): void;

    setURL(uploadUrl: string): void;

    setWidth(width: number): void;

    unload(): void;

    upload(): void;
}

type dhtmlXWindowsEventName =
    'onBeforeMoveStart'
    | 'onBeforeResizeStart'
    | 'onClose'
    | 'onContentLoaded'
    | 'onFocus'
    | 'onHelp'
    | 'onHide'
    | 'onMaximize'
    | 'onMinimize'
    | 'onMoveCancel'
    | 'onMoveFinish'
    | 'onParkDown'
    | 'onParkUp'
    | 'onResizeCancel'
    | 'onResizeFinish'
    | 'onShow'
    | 'onStick'
    | 'onUnStick';

declare class dhtmlXWindows {
    attachContextMenu(config: any): dhtmlXMenuObject;

    attachEvent(name: dhtmlXWindowsEventName, handler: ICallable): number;

    attachViewportTo(objId: string): void;

    createWindow(id: string, x: number, y: number, width: number, height: number): void;

    detachContextMenu(menuObj: any): void;

    detachEvent(id: number): void;

    enableAutoViewport(): void;

    findByText(text: string): any[];

    forEachWindow(handler: ICallable): void;

    getBottommostWindow(): any;

    getContextMenu(): dhtmlXMenuObject;

    getEffect(): void;

    getTopmostWindow(visibleOnly: boolean): any;

    isWindow(id: string): boolean;

    setEffect(): void;

    setImagePath(): void;

    setSkin(skin: string): void;

    setViewport(x: number, y: number, width: number, height: number, parentObj: HTMLElement): void;

    unload(): void;

    window(id: string): dhtmlXWindowsCell;

}

type dhtmlXWindowsButtonEventName = 'onClick';

declare class dhtmlXWindowsButton {
    attachContextMenu(config: any): dhtmlXMenuObject;

    attachEvent(name: dhtmlXWindowsButtonEventName, handler: ICallable): number;

    detachContextMenu(menuObj: any): void;

    detachEvent(id: number): void;

    disable(): void;

    enable(): void;

    getContextMenu(): dhtmlXMenuObject;

    hide(): void;

    isEnabled(): boolean;

    isHidden(): boolean;

    setCss(style: string): void;

    show(): void;

}

type dhtmlXWindowsCellEventName =
    'onBeforeMoveStart'
    | 'onBeforeResizeStart'
    | 'onClose'
    | 'onContentLoaded'
    | 'onFocus'
    | 'onHelp'
    | 'onHide'
    | 'onMaximize'
    | 'onMinimize'
    | 'onMoveCancel'
    | 'onMoveFinish'
    | 'onParkDown'
    | 'onParkUp'
    | 'onResizeCancel'
    | 'onResizeFinish'
    | 'onShow'
    | 'onStick'
    | 'onUnStick';

declare class dhtmlXWindowsCell extends dhtmlXCell {
    addUserButton(id: string, pos: number, title: string, label: string): void;

    adjustPosition(): void;

    allowMove(): void;

    allowPark(): void;

    allowResize(): void;

    appendObject(id: any): void;

    attachAccordion(conf: any): dhtmlXAccordion;

    attachCarousel(width: number, height: number, conf: any): dhtmlXCarousel;

    attachChart(conf: any): dhtmlXChart;

    attachContextMenu(config: any): dhtmlXMenuObject;

    attachDataView(conf: any): dhtmlXDataView;

    attachEditor(): dhtmlXEditor;

    attachEvent(name: dhtmlXWindowsCellEventName, handler: ICallable): number;

    attachForm(conf: any): dhtmlXForm;

    attachGrid(): dhtmlXGridObject;

    attachHTMLString(htmlString: string): void;

    attachLayout(conf: any): dhtmlXLayoutObject;

    attachList(conf: any): dhtmlXList;

    attachMap(opts?: any): any;

    attachMenu(conf: any): dhtmlXMenuObject;

    attachObject(obj: any): void;

    attachPortal(conf: any): dhtmlXPortal;

    attachRibbon(conf: any): dhtmlXRibbon;

    attachScheduler(day: Date, mode: string, contId: string, scheduler: dhtmlXScheduler): dhtmlXScheduler;

    attachSidebar(conf: any): dhtmlXSideBar;

    attachStatusBar(conf: any): { [key: string]: any; };

    attachTabbar(conf: any): dhtmlXTabBar;

    attachToolbar(conf: any): dhtmlXToolbarObject;

    attachTree(rootId: any): dhtmlXTreeObject;

    attachTreeView(conf: any): dhtmlXTreeViewObject;

    attachURL(url: string, ajax: boolean, postData: any): void;

    attachVault(conf: any): dhtmlXVaultObject;

    bringToBottom(): void;

    bringToTop(): void;

    button(id: string): any;

    center(): void;

    centerOnScreen(): void;

    clearIcon(): void;

    close(): void;

    denyMove(): void;

    denyPark(): void;

    denyResize(): void;

    detachContextMenu(menuObj: any): void;

    detachEvent(id: number): void;

    detachMenu(): void;

    detachObject(remove: boolean, moveTo: string | number): void;

    detachRibbon(): void;

    detachStatusBar(): void;

    detachToolbar(): void;

    getAttachedMenu(): dhtmlXMenuObject;

    getAttachedObject(): any;

    getAttachedRibbon(): dhtmlXRibbon;

    getAttachedStatusBar(): any;

    getAttachedToolbar(): dhtmlXToolbarObject;

    getContextMenu(): dhtmlXMenuObject;

    getDimension(): any[];

    getFrame(): void;

    getIcon(): void;

    getId(): any;

    getMaxDimension(): any[];

    getMinDimension(): number;

    getPosition(): any[];

    getText(): string;

    getViewName(): string;

    hide(): void;

    hideHeader(): void;

    hideMenu(): void;

    hideRibbon(): void;

    hideStatusBar(): void;

    hideToolbar(): void;

    isHidden(): boolean;

    isMaximized(): boolean;

    isModal(): boolean;

    isMovable(): boolean;

    isOnBottom(): boolean;

    isOnTop(): boolean;

    isParkable(): boolean;

    isParked(): boolean;

    isResizable(): boolean;

    isSticked(): boolean;

    keepInViewport(state: boolean): void;

    maximize(): void;

    minimize(): void;

    park(): void;

    progressOff(): void;

    progressOn(): void;

    reloadURL(): void;

    removeUserButton(id: string): void;

    restoreIcon(): void;

    setDimension(width: number, height: number): void;

    setIcon(): void;

    setIconCss(style: string): void;

    setMaxDimension(maxWidth: number, maxHeight: number): void;

    setMinDimension(minWidth: number, minHeight: number): void;

    setModal(state: boolean): void;

    setPosition(x: number, y: number): void;

    setText(text: string): void;

    setToFullScreen(): void;

    show(): void;

    showHeader(): void;

    showInnerScroll(): void;

    showMenu(): void;

    showRibbon(): void;

    showStatusBar(): void;

    showToolbar(): void;

    showView(name: string): boolean;

    stick(): void;

    unloadView(name: string): void;

    unstick(): void;

}
