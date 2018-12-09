///<reference path="../dhtmlx/dhtmlx.d.ts"/>

function appInit(): void {
    let layout = new dhtmlXLayoutObject({parent: "layoutMaster", pattern: "3W"});

    let disassembly = layout.cells("b");
    disassembly.setText("Disassembly");
    disassembly.appendObject("disassemblyText");

    let controlFlow = layout.cells("c");
    controlFlow.setText("Control flow graph");
    controlFlow.appendObject("decompileContainer");

    let decompileGraph = new vis.Network(document.getElementById('decompileContainer'), {'nodes': [], 'edges': []}, {
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
    
    let hexFormatter = function (row, cell, value, columnDef, dataContext) {
        return !value ? '' : "0x" + parseInt(value).toString(16);
    };

    let dataView = new Slick.Data.DataView();
    let grid = new Slick.Grid('#disassemblyText', dataView, [
        {id: "address", name: "Address", field: "address", formatter: hexFormatter},
        {id: "code", name: "Code", field: "text", width: 300},
        {id: "jumptarget", name: "Jump Target", field: "jumpTarget", width: 200, formatter: hexFormatter}
    ], {
        enableCellNavigation: false,
        enableColumnReorder: false,
        autoHeight: true,
        autoEdit: false,
        rowHeight: 20
    });

    dataView.onRowCountChanged.subscribe((e, args) => {
        grid.updateRowCount();
        grid.render();
    });
    dataView.onRowsChanged.subscribe((e, args) => {
        grid.invalidateRows(args.rows);
        grid.render();
    });
    
    let symbols = layout.cells("a");
    symbols.setText("Symbols");
    symbols.setWidth(300);
    let symbolsTree = symbols.attachTreeView();
    symbolsTree.attachEvent("onSelect", (id: string, mode: boolean) => {
        let address = symbolsTree.getUserData(id)["address"];
        let instructions = dhx.s2j(dhx.ajax.getSync("api/assembly/instructions/" + address + "/200").xmlDoc.responseText);

        dataView.beginUpdate();
        dataView.setItems(instructions, 'address');
        dataView.endUpdate();

        grid.resizeCanvas();

        let graph = dhx.s2j(dhx.ajax.getSync("api/assembly/decompile/" + address).xmlDoc.responseText);
        console.debug(graph);

        decompileGraph.setData(graph);
        decompileGraph.redraw();
    });
    symbolsTree.loadStruct("api/symbols/callees"); // populate initial data if there's already a project loaded

    let menu = layout.attachMenu();
    menu.setIconsPath("icons/");
    menu.loadStruct("layouts/menu.xml");

    let uploadUrl = "";
    let postUploadAction: Function = null;
    let fileInput: HTMLInputElement = document.getElementById("fileInput") as HTMLInputElement;
    fileInput.addEventListener("change", function (event) {
        event.preventDefault();

        if (this.files.length != 1)
            return;

        let windowSystem = new dhtmlXWindows();
        windowSystem.createWindow("uploadInfo", 10, 10, 400, 100);
        let uploadInfo = windowSystem.window("uploadInfo");
        uploadInfo.hideHeader();
        uploadInfo.setModal(true);
        uploadInfo.stick();
        uploadInfo.denyMove();
        uploadInfo.denyResize();
        uploadInfo.attachHTMLString("<p>Please wait...</p>");
        uploadInfo.show();
        uploadInfo.center();
        uploadInfo.progressOn();

        let form = new FormData();
        let xhr = new XMLHttpRequest();

        let file = this.files[0];
        form.append('file', file, file.name);

        xhr.onload = () => {
            windowSystem.unload();
            postUploadAction();
        };

        xhr.onprogress = event => {
            let p = event.loaded * 100.0 / event.total;
            uploadInfo.attachHTMLString("<p>Please wait...</p><p>" + p + "%</p>");
        };

        xhr.open('POST', uploadUrl);
        xhr.send(form);
    });

    menu.attachEvent('onClick', id => {
        if (id === 'ftLoadSym') {
            uploadUrl = 'api/upload/sym';
            postUploadAction = () => {
                symbolsTree.loadStruct("api/symbols/callees");
            };
            fileInput.click();
        }
        else if (id === 'ftLoadExe') {
            uploadUrl = 'api/upload/exe';
            postUploadAction = () => {
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
