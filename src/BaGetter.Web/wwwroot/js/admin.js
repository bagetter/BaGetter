(function () {
  "use strict";

  var trigger = null;

  if ($("#adminStatus").length) {
    $("#adminStatus").trigger("focus");
  }

  $(".js-direct-operation").closest("form").on("submit", function () {
    var submit = $(this).find(".js-direct-operation");
    submit.prop("disabled", true).text(submit.data("progress-label"));
  });

  $(".js-package-operation").on("click", function () {
    var button = $(this);
    var operation = button.data("operation");
    var id = button.data("id");
    var version = button.data("version");
    var isDelete = operation === "delete";
    var isRename = operation === "rename";
    trigger = this;

    $("#packageOperation").val(operation);
    $("#packageId").val(id);
    $("#packageVersion").val(version);
    $("#targetId").val("");
    $("#confirmation").val("");
    $("#targetIdGroup").toggle(!isDelete);
    $("#confirmationGroup").toggle(isDelete);
    $("#targetId").prop("required", !isDelete);
    $("#confirmation").prop("required", isDelete);

    var verb = operation.charAt(0).toUpperCase() + operation.slice(1);
    $("#packageOperationTitle").text(isDelete ? "Permanently delete package" : verb + " package");
    $("#packageOperationSummary").text(
      isDelete
        ? "Permanently delete " + id + " " + version + "."
        : verb + " " + id + " " + version + ".");
    $("#packageOperationWarning")
      .toggle(isDelete || isRename)
      .text(
        isDelete
          ? "This permanently removes package metadata and stored package content. It cannot be undone."
          : "Rename creates the package under the new ID, then permanently deletes the source. Existing references to the old ID may stop working.");
    $("#targetIdHelp").text(
      operation === "copy"
        ? "The version remains unchanged. The source package is not modified."
        : "");
    $("#confirmationLabel").text("Type " + id + " to confirm");
    $("#packageOperationSubmit")
      .text(isDelete ? "Permanently delete" : verb + " package")
      .data("default-label", isDelete ? "Permanently delete" : verb + " package")
      .data("progress-label", isDelete ? "Deleting..." : verb + "ing...")
      .prop("disabled", isDelete)
      .toggleClass("btn-danger", isDelete)
      .toggleClass("btn-primary", !isDelete);
  });

  $("#confirmation").on("input", function () {
    var matches = $.trim($(this).val()) === $("#packageId").val();
    $("#packageOperationSubmit").prop("disabled", !matches);
  });

  $("#packageOperationModal").on("shown.bs.modal", function () {
    var operation = $("#packageOperation").val();
    $(operation === "delete" ? "#confirmation" : "#targetId").trigger("focus");
  });

  $("#packageOperationModal").on("hidden.bs.modal", function () {
    $("#packageOperationSubmit")
      .prop("disabled", false)
      .text($("#packageOperationSubmit").data("default-label") || "Continue");
    if (trigger) {
      $(trigger).trigger("focus");
    }
  });

  $("#packageOperationModal form").on("submit", function () {
    var submit = $("#packageOperationSubmit");
    submit.prop("disabled", true).text(submit.data("progress-label"));
  });
})();
