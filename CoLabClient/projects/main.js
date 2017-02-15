var $own = $("#own-projects"), $collab = $("#collab-projects");
function setupProjects(p) {
    for (var i = 0; i < p.own.length; i++){
        insertProject($own, p.own[i]);
    }
    for (i = 0; i < p.collab.length; i++){
        insertProject($collab, p.collab[i]);
    }
}

function insertProject($paren, proj) {
    var url = "/project/" + proj.Id;
    $paren.append("<li><a href='"+url+"' >"+proj.Name+"</a></li>");
}
$("#createNew").click(function () {
    window.location = "http://localhost:5005" + "/createproject";
});

setupProjects(projects);