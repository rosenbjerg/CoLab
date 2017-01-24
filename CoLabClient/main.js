var Range = require("ace/range").Range;
var editor, session, ws, cmenuItemClicked, inserting = false, writing = false, pid = "davn84579owe4509n80";
var $code = $("#code"), $pos = $("#position"), $filePanel = $("#project-explorer-files");
var dirRegExp = /^[\w]+$/, fileRegExp = /^[\w\.]+$/;

var modelist = ace.require("ace/ext/modelist");
ace.require("ace/ext/language_tools");
var code = $.get("http://localhost:5005/file/test.txt", function (data) {
    $code.text(data);
    editor = ace.edit("code");
    session = editor.getSession();
    editor.$blockScrolling = Infinity;
    session.setMode(modelist.getModeForPath("test.html").mode);
    editor.setOptions({
        enableBasicAutocompletion: true,
        enableSnippets: true,
        enableLiveAutocompletion: true
    });
    editor.setTheme("ace/theme/monokai");
    editor.focus();
    $("#ace_settingsmenu, #kbshortcutmenu").css("background-color", "#616161");
    ws = new WebSocket("ws://localhost:5005/test.txt");

    session.selection.on('changeCursor', function() {
        var c = editor.selection.getCursor();
        setPosition(c.row +1, c.column);
    });

    session.on('change', function(e) {
        if (!inserting)
        {
            e.sender = sessionStorage.getItem("id");
            ws.send(JSON.stringify(e));
        }
    });

    ws.onmessage = function (msg) {
        var data = JSON.parse(msg.data);
        if (data.sender == sessionStorage.getItem("id"))
            return;
        inserting = true;
        if (data.action == "insert")
            editor.session.insert(data.start, data.lines.join("\n"));
        else
            editor.session.replace(new Range(data.start.row, data.start.column, data.end.row, data.end.column), "");
        inserting = false;
    };
});

function setPosition(l,c) {
    $pos.text(l + ":" + c);
}

function openFile() {
    
}

window.addEventListener("beforeunload", function () {
    ws.close();
});

if (sessionStorage.getItem("id") === null){
    sessionStorage.setItem("id", Math.random());
}



// If the document is clicked somewhere
$(document).bind("mousedown", function (e) {
    if (!$(e.target).parents(".custom-menu").length > 0) {
        $(".custom-menu").hide(100);
    }
});

function addFiles(rootDir, startPath) {
    var retVal = "";
    if (rootDir.Name != ""){
        startPath += "/" + rootDir.Name;
    }
    if (rootDir.Dirs){
        for (var i = 0; i < rootDir.Dirs.length; i++){
            var id = startPath + "/" + rootDir.Dirs[i].Name;
            retVal += '<li><a name="' + id + '" class="panel-dir" >' + rootDir.Dirs[i].Name + '</a><ul>'+ addFiles(rootDir.Dirs[i], startPath) + '</ul></li>';
        }
    }
    if (rootDir.Files){
        for (i = 0; i < rootDir.Files.length; i++){
            id = rootDir.Files[i].Id;
            retVal += '<li><a id="' + id + '" onclick="hfc(\'' + id + '\')">' + rootDir.Files[i].Name + '</a></li>'
        }
    }

    return retVal;
}

// Handle file click
function hfc(file) {
    console.log("file clicked: " + file.trim("/"));
}

function loadMenu() {
    $filePanel.append(addFiles(, ""));

    $filePanel.find("a").on("click", function (e) {
        if ($(this).parent().has("ul")) {
            e.preventDefault();
        }
        $(this).next('ul').slideToggle();
    });

    $("#project-explorer-header").find("> a").bind("contextmenu", function (event) {
        if (event.ctrlKey) return;
        event.preventDefault();
        cmenuItemClicked = $(this).attr('name');
        $("#rootdir-rightclick").finish().toggle(100).css({
            top: event.pageY + "px",
            left: event.pageX + "px"
        });
    });

    $filePanel.find("li a:not(.panel-dir)").bind("contextmenu", function (event) {
        if (event.ctrlKey) return;
        event.preventDefault();
        cmenuItemClicked = $(this).attr('name');
        $("#file-rightclick").finish().toggle(100).css({
            top: event.pageY + "px",
            left: event.pageX + "px"
        });
    });

    $filePanel.find("li a.panel-dir").bind("contextmenu", function (event) {
        if (event.ctrlKey) return;
        event.preventDefault();
        cmenuItemClicked = $(this).attr('name');
        $("#dir-rightclick").finish().toggle(100).css({
            top: event.pageY + "px",
            left: event.pageX + "px"
        });
    });

}

loadMenu();

var $dialog = $("#dialogTarget");
function openInputDialog(title, msg, val, html, checkFunc) {
    $dialog.text(msg + " ");
    $dialog.append(html);
    $("#dialogInput").val(val);
    $dialog.dialog({
        resizable: false,
        height: "auto",
        title: title,
        width: "auto",
        modal: true,
        buttons: {
            "OK": function() {
                if (!checkFunc($("#dialogInput").val())) return;
                $dialog.dialog( "close" );
                $dialog.empty();
            },
            Cancel: function() {
                $dialog.dialog( "close" );
                $dialog.empty();
            }
        }
    });
}
// If the menu element is clicked
$(".custom-menu li").click(function(){
    if (cmenuItemClicked.charAt(0) == '/') cmenuItemClicked = cmenuItemClicked.substr(1);
    // This is the triggered action name
    switch($(this).attr("data-action")) {
        case "importFile":
            openInputDialog("Import file", "", "", "<form enctype='multipart/form-data' action='/project/"+ pid +"/upload' method='post'>" +
            "<input id='dialogInput' type='file' required multiple/></form>", function (inp) {
                inp = inp.substr(inp.lastIndexOf("/") + 1);
                inp = inp.substr(inp.lastIndexOf("\\") + 1);
                if (!fileRegExp.test(inp)) {
                    alert("Invalid file name: " + inp);
                    return false;
                }
                var form = $("#dialogTarget").find("form")[0];

                console.log(form);
                var fd = new FormData(document.querySelector("form"));
                fd.append("CustomField", "This is some extra data");
                $.ajax({
                    url: "/project/" + pid + "/upload",
                    type: "POST",
                    data: fd,
                    processData: false,  // tell jQuery not to process the data
                    contentType: false   // tell jQuery not to set contentType
                });

                // form.submit(function(ev) {
                //     oData.append("CustomField", "This is some extra data");
                //     var oReq = new XMLHttpRequest();
                //     oReq.open("POST", "stash.php", true);
                //     oReq.onload = function(oEvent) {
                //         if (oReq.status == 200) {
                //             oOutput.innerHTML = "Uploaded!";
                //         } else {
                //             oOutput.innerHTML = "Error " + oReq.status + " occurred when trying to upload your file.<br \/>";
                //         }
                //     };
                //
                //     oReq.send(oData);
                //     ev.preventDefault();
                //
                //     // e.preventDefault();
                //     // $(this).ajaxSubmit().done(function (data) {
                //     //     var x = JSON.parse(data);
                //     //     alert("Success : " + x);
                //     // }).fail(function (data) {
                //     //     var x = JSON.parse(data);
                //     //     alert("Error : " + x);
                //     // });
                // });
                return true;
            });
            break;
        case "addNewDir":
            openInputDialog("New directory", cmenuItemClicked + "/", "", "<input id='dialogInput' type='text'/>", function (inp) {
                return dirRegExp.test(inp);
            });
            break;
        case "addNewFile":
            openInputDialog("New file", cmenuItemClicked + "/", "", "<input id='dialogInput' type='text'/>", function (inp) {
                return fileRegExp.test(inp);
            });
            break;
        case "rename":
            var i = cmenuItemClicked.lastIndexOf("/");
            var s1 = cmenuItemClicked.substr(0, i+1);
            var sr = cmenuItemClicked.substr(i+1);
            openInputDialog("Rename", s1, sr, "<input id='dialogInput' type='text'/>", function (inp) {
                return fileRegExp.test(inp);
            });
            break;
        case "delete":
            $dialog.text("Are you sure you want to delete '" + cmenuItemClicked + "' ?");
            $dialog.dialog({
            resizable: false,
            title:"Delete file?",
            height: "auto",
            width: "auto",
            modal: true,
            buttons: {
                "Yes": function() {
                    //Rename file request
                    $dialog.dialog( "close" );
                    $dialog.html("");
                },
                Cancel: function() {
                    $dialog.dialog( "close" );
                    $dialog.html("");
                }
            }
        });
            break;
    }
    $(".custom-menu").hide(100);
});