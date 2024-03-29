﻿$(document).ready(function () {
    var processId = Number($("#hdnProcessId").val());

    getSourceDocuments();

    function getSourceDocuments() {
        $.ajax({
            type: "GET",
            url: "_SourceDocumentDetails",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: { "processId": processId },
            success: function (result) {
                $("#sourceDocuments").html(result);

                $(".attachLinkedDocumentSpinnerContainer").hide();

                applyCollapseIconHandler();
                applyAttachLinkedDocumentHandlers();
                applySearchSourceHandler();
                applyAddSourceHandler();
            },
            error: function (error) {
                $("#sourceDocumentsError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Source Documents. Please try again later.</div>");
            }
        });
    }

    function applyAttachLinkedDocumentHandlers() {
        $(".attachLinkedDocument").on("click", function (e) {
            $(this).parent().siblings(".attachLinkedDocumentSpinnerContainer").show();
            $(this).parent().hide();

            var linkedSdocId = Number($(this).data("linkedsdocid"));
            var processId = Number($(this).data("processid"));
            var correlationId = $(this).data("correlationid");

            $.ajax({
                type: "POST",
                url: "_SourceDocumentDetails/?handler=AttachLinkedDocument",
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: {
                    "linkedSdocId": linkedSdocId,
                    "processId": processId,
                    "correlationId": correlationId
                },
                success: function (result) {
                    getSourceDocuments();
                },
                error: function (error) {
                    //TODO: Implement error dialogs
                    $("#assignTasksError")
                        .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to create new assign task section.</div>");
                }
            });
        });
    }

    function searchSource() {

        var enteredSdocId = $("#txtSourceDocumentId").val().trim();

        if (enteredSdocId.match(/^[1-9][0-9]*$/) === null) {
            $("#addDatabaseSourceDocument .dialog.success").collapse("hide");
            $("#addDatabaseSourceDocument .dialog.warning").collapse("hide");
            $("#addSourceErrorMessage").text("Please enter a numeric Source Document ID.");
            $("#addDatabaseSourceDocument .dialog.error").collapse("show");
            return;
        }

        var sdocId = Number(enteredSdocId);

        hideAddSourceDialogs();

        $.ajax({
            type: "GET",
            url: "_SourceDocumentDetails/?handler=DatabaseSourceDocumentData",
            beforeSend: function (xhr) {
                xhr.setRequestHeader("XSRF-TOKEN", $('input:hidden[name="__RequestVerificationToken"]').val());
            },
            contentType: "application/json; charset=utf-8",
            data: { "sdocId": sdocId },
            success: function (data) {
                if (data === null) {
                    $("#addDatabaseSourceDocument .dialog.success").collapse("hide");
                    $("#addDatabaseSourceDocument .dialog.warning").collapse("hide");
                    $("#addSourceErrorMessage").text("Source Document ID " + sdocId + " not found.");
                    $("#addDatabaseSourceDocument .dialog.error").collapse("show");
                    return;
                }

                $("#addDatabaseSourceDocument .dialog.success").collapse("show");
                $("#addSourceSdocId").text(data.sdocId);
                $("#addSourceName").text(data.name);
                $("#addSourceDocType").text(data.documentType);
                $("#addDatabaseSourceDocument .dialog.error").collapse("hide");
                $("#addDatabaseSourceDocument .dialog.warning").collapse("hide");
                $("#addSourceErrorMessage").text("");
            },
            error: function () {
                $("#sourceDocumentsError")
                    .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to load Source Documents. Please try again later.</div>");
            }
        });
    }

    function applyCollapseIconHandler() {
        $(".collapse").on("show.bs.collapse", function (e) {
            var el = $(e.currentTarget).prev("[data-toggle='collapse']");
            var icon = $(el).find("i.fa.fa-plus");
            icon.removeClass("fa-plus").addClass("fa-minus");
        });

        $(".collapse").on("hide.bs.collapse", function (e) {
            var el = $(e.currentTarget).prev("[data-toggle='collapse']");
            var icon = $(el).find("i.fa.fa-minus");
            icon.removeClass("fa-minus").addClass("fa-plus");
        });
    }

    function applySearchSourceHandler() {

        $("#btnSearchSource").on("click", function (e) {
            searchSource();
        });

        $("#txtSourceDocumentId").keypress(function (e) {
            if (e.keyCode !== 13) { //If not enter key then return
                return;
            }

            e.preventDefault(); //Prevent 'Done' form submission when user hit enter
            searchSource();
        });
    }

    function hideAddSourceDialogs() {
        $("#sourceDocumentsError").html("");
        $("#addDatabaseSourceDocument .dialog.success").collapse("hide");
        $("#addDatabaseSourceDocument .dialog.warning").collapse("hide");
        $("#addDatabaseSourceDocument .dialog.error").collapse("hide");
    }

    function applyAddSourceHandler() {

        $("#btnAddSource").on("click", function (e) {

            //success
            $("#addDatabaseSourceDocument .dialog.success").collapse("hide");

            var sdocId = Number($("#addSourceSdocId").text());
            var sourceName = $("#addSourceName").text();
            var docType = $("#addSourceDocType").text();
            var correlationId = $(this).data("correlationid");

            $.ajax({
                type: "POST",
                url: "_SourceDocumentDetails/?handler=AddSourceFromSdra",
                beforeSend: function (xhr) {
                    xhr.setRequestHeader("RequestVerificationToken", $('input:hidden[name="__RequestVerificationToken"]').val());
                },
                data: {
                    "sdocId": sdocId,
                    "docName": sourceName,
                    "docType": docType,
                    "processId": processId,
                    "correlationId": correlationId
                },
                success: function (result) {
                    $("#addSourceErrorMessage").val();
                    $("#addDatabaseSourceDocument .dialog.error").collapse("hide");
                    getSourceDocuments();
                },
                error: function (xhr, error) {
                    if (xhr.status === 405) {
                        $("#addSourceWarningMessage").text("Sdoc " + sdocId + " already added.");
                        $("#addDatabaseSourceDocument .dialog.warning").collapse("show");
                        return;
                    }

                    //TODO: Implement error dialogs
                    $("#assignTasksError")
                        .html("<div class=\"alert alert-danger\" role=\"alert\">Failed to create new assign task section.</div>");
                }
            });
        });
    }
});