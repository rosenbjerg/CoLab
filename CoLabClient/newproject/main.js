var $form = $("form");
var $sea = $("#search"), $sres = $("#searchResults"), $cols = $("#collabs");
$form.submit(function (ev) {
    ev.preventDefault();
    // $form.find("input").attr("disabled", true);
    var sd = $("#search").val().split(",");
    var obj = {
        pn: $("#pn").val(),
        pd: $("#pd").val(),
        co: sd
    };

    $.ajax({
        url:"http://localhost:5005" + "/createproject",
        type:"POST",
        data:JSON.stringify(obj),
        contentType:"application/json; charset=utf-8",
        success: function(data){
            console.log(data);
            if (data != "no"){
                window.location = "/project/" + data;
            }
            else
            {
                // $("form").find("input").attr("disabled", false);
            }
        }
    });
    alert("sent");
});
var slen = 0;
$sea.change(function () {
    var t = $sea.val();
    var olen = slen;
    slen = t.length;
    if (slen < 4 || olen >= slen)
        return;
    // Get search results
    // var rs = [
    //     {Id: 342442, Name: "Jens"},
    //     {Id: 533545, Name: "Peter"},
    //     {Id: 345345, Name: "Søren"}
    // ];



});
