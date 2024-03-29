﻿$(document).ready(function () {

    getComments();


});

function focusTextAreaHandler() {
    $("#addCommentModal").on("shown.bs.modal",
        function () {
            $("#txtComment").focus();
        });
}

function getComments() {
    var processId = { "processId": $("#hdnProcessId").val() };

    $.ajax({
        type: "GET",
        url: "_Comments",
        beforeSend: function (xhr) {
            xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
        },
        contentType: "application/json; charset=utf-8",
        data: processId,
        success: function (result) {
            $("#existingComments").html(result);
            addCommentsHandler();
            focusTextAreaHandler();
            postCommentsHandler();
        },
        error: function (error) {
            $("#AddCommentError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load comments.</div>");
        }
    });
}

function addCommentsHandler() {
    $("#btnAddComment").on("click", function () {
        $("#btnPostComment").prop("disabled", false);
        $("#AddCommentError").html("");
        $("#addCommentModal").modal("show");
    });
}

function postCommentsHandler() {
    $("#btnPostComment").on("click", function () {
        $("#btnPostComment").prop("disabled", true);

        if ($('#txtComment').val() === "") {
            $("#AddCommentError")
                .html("<div class=\"alert alert-danger\" role=\"alert\">Please enter a comment.</div>");
            $('#txtComment').focus();

            $("#btnPostComment").prop("disabled", false);
        } else {

            var processId = Number($("#hdnProcessId").val());
            var newCommentMessage = $("#txtComment").val();

            $.ajax({
                type: "POST",
                url: "_Comments/?handler=Comments",
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: {
                    "ProcessId": processId,
                    "newCommentMessage": newCommentMessage
                },
                success: function (result) {
                    $("#addCommentModal").modal("hide");
                    $("body").removeClass("modal-open");
                    $(".modal-backdrop").remove();
                    getComments();
                },
                error: function (error) {
                    console.log(error);

                    $("#AddCommentError")
                        .html("<div class=\"alert alert-danger\" role=\"alert\">Error adding comment. Please try again later.</div>");

                    $("#btnPostComment").prop("disabled", false);
                }
            });
        }
    });

}