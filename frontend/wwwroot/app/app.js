///<reference path="../dhtmlx/dhtmlx.d.ts"/>
function appInit() {
    var layout = new dhtmlXLayoutObject({ parent: "layoutMaster", pattern: "3W" });
    var disassembly = layout.cells("b");
    disassembly.setText("Disassembly");
    disassembly.appendObject("disassemblyText");
    var controlFlow = layout.cells("c");
    controlFlow.setText("Control flow graph");
    controlFlow.appendObject("decompileContainer");
    var decompileGraph = new vis.Network(document.getElementById('decompileContainer'), { 'nodes': [], 'edges': [] }, {
        "edges": {
            "smooth": {
                "type": "cubicBezier",
                "forceDirection": "vertical",
                "roundness": 0.35
            }
        },
        "physics": {
            "repulsion": {
                "springLength": 350,
                "nodeDistance": 500
            },
            "minVelocity": 0.75,
            "solver": "repulsion"
        }
        /*manipulation: false,
        layout: {
            hierarchical: {
                enabled: true,
                levelSeparation: 300
            }
        },
        physics: {
            hierarchicalRepulsion: {
                nodeDistance: 300
            }
         }*/
    });
    var hexFormatter = function (row, cell, value, columnDef, dataContext) {
        return !value ? '' : "0x" + parseInt(value).toString(16);
    };
    var dataView = new Slick.Data.DataView();
    var grid = new Slick.Grid('#disassemblyText', dataView, [
        { id: "address", name: "Address", field: "address", formatter: hexFormatter },
        { id: "code", name: "Code", field: "text", width: 300 },
        { id: "jumptarget", name: "Jump Target", field: "jumpTarget", width: 200, formatter: hexFormatter }
    ], {
        enableCellNavigation: false,
        enableColumnReorder: false,
        autoHeight: true,
        autoEdit: false,
        rowHeight: 20
    });
    dataView.onRowCountChanged.subscribe(function (e, args) {
        grid.updateRowCount();
        grid.render();
    });
    dataView.onRowsChanged.subscribe(function (e, args) {
        grid.invalidateRows(args.rows);
        grid.render();
    });
    var symbols = layout.cells("a");
    symbols.setText("Symbols");
    symbols.setWidth(300);
    var symbolsTree = symbols.attachTreeView();
    symbolsTree.attachEvent("onSelect", function (id, mode) {
        var address = symbolsTree.getUserData(id)["address"];
        var instructions = dhx.s2j(dhx.ajax.getSync("api/assembly/instructions/" + address).xmlDoc.responseText);
        dataView.beginUpdate();
        dataView.setItems(instructions, 'address');
        dataView.endUpdate();
        grid.resizeCanvas();
        var graph = dhx.s2j(dhx.ajax.getSync("api/assembly/decompile/" + address).xmlDoc.responseText);
        console.debug(graph);
        decompileGraph.setData(graph);
        decompileGraph.redraw();
    });
    symbolsTree.loadStruct("api/symbols/callees"); // populate initial data if there's already a project loaded
    var menu = layout.attachMenu();
    menu.setIconsPath("icons/");
    menu.loadStruct("layouts/menu.xml");
    var uploadUrl = "";
    var postUploadAction = null;
    var fileInput = document.getElementById("fileInput");
    fileInput.addEventListener("change", function (event) {
        event.preventDefault();
        if (this.files.length != 1)
            return;
        var windowSystem = new dhtmlXWindows();
        windowSystem.createWindow("uploadInfo", 10, 10, 400, 100);
        var uploadInfo = windowSystem.window("uploadInfo");
        uploadInfo.hideHeader();
        uploadInfo.setModal(true);
        uploadInfo.stick();
        uploadInfo.denyMove();
        uploadInfo.denyResize();
        uploadInfo.attachHTMLString("<p>Please wait...</p>");
        uploadInfo.show();
        uploadInfo.center();
        uploadInfo.progressOn();
        var form = new FormData();
        var xhr = new XMLHttpRequest();
        var file = this.files[0];
        form.append('file', file, file.name);
        xhr.onload = function () {
            windowSystem.unload();
            postUploadAction();
        };
        xhr.onprogress = function (event) {
            var p = event.loaded * 100.0 / event.total;
            uploadInfo.attachHTMLString("<p>Please wait...</p><p>" + p + "%</p>");
        };
        xhr.open('POST', uploadUrl);
        xhr.send(form);
    });
    menu.attachEvent('onClick', function (id) {
        if (id === 'ftLoadSym') {
            uploadUrl = 'api/upload/sym';
            postUploadAction = function () {
                symbolsTree.loadStruct("api/symbols/callees");
            };
            fileInput.click();
        }
        else if (id === 'ftLoadExe') {
            uploadUrl = 'api/upload/exe';
            postUploadAction = function () {
                dataView.beginUpdate();
                dataView.setItems([]);
                dataView.endUpdate();
            };
            fileInput.click();
        }
    });
    /*let toolbar = layout.attachToolbar();
     toolbar.setIconsPath("icons/");
     toolbar.loadStruct("layouts/toolbar.xml");*/
}
