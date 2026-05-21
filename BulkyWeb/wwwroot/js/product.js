let dataTable;

$(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = new DataTable('#tblData', {
        ajax: {
            url: '/Admin/Product/GetAll',
            type: 'GET'
        },
        columns: [
            { data: 'title', width: '25%' },
            { data: 'isbn', width: '15%' },
            {
                data: 'listPrice',
                width: '10%',
                render: function (data) {
                    return data != null ? '$' + Number(data).toFixed(2) : '';
                }
            },
            { data: 'author', width: '20%' },
            { data: 'category.name', defaultContent: '', width: '15%' },
            {
                data: 'id',
                orderable: false,
                render: function (data) {
                    return (
                        '<div class="w-75 btn-group" role="group">' +
                        '<a href="/Admin/Product/Upsert?id=' +
                        data +
                        '" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i> Edit</a>' +
                        '<a onclick="Delete(\'/Admin/Product/Delete/' +
                        data +
                        '\')" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i> Delete</a>' +
                        '</div>'
                    );
                },
                width: '25%'
            }
        ]
    });
}

function Delete(url) {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
    }).then(function (result) {
        if (result.isConfirmed) {
            $.ajax({
                type: 'DELETE',
                url: url,
                dataType: 'json',
                success: function (data) {
                    if (data.success) {
                        if (dataTable && dataTable.ajax && typeof dataTable.ajax.reload === 'function') {
                            dataTable.ajax.reload();
                        }
                        Swal.fire('Deleted!', data.message, 'success');
                    } else {
                        Swal.fire('Error!', data.message, 'error');
                    }
                },
                error: function () {
                    Swal.fire('Error!', 'Something went wrong while deleting.', 'error');
                }
            });
        }
    });
}
