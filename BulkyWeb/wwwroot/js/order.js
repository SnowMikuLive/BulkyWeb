let dataTable;

$(function () {
    const url = window.location.search;
    if (url.includes('inprocess')) {
        loadDataTable('inprocess');
    } else if (url.includes('completed')) {
        loadDataTable('completed');
    } else if (url.includes('pending')) {
        loadDataTable('pending');
    } else if (url.includes('approved')) {
        loadDataTable('approved');
    } else {
        loadDataTable('all');
    }
});

function loadDataTable(status) {
    dataTable = new DataTable('#tblData', {
        ajax: {
            url: '/Admin/Order/GetAll?status=' + status,
            type: 'GET'
        },
        columns: [
            { data: 'id', width: '5%' },
            { data: 'name', width: '25%' },
            { data: 'phoneNumber', width: '20%' },
            { data: 'applicationUser.email', defaultContent: '', width: '20%' },
            { data: 'orderStatus', width: '10%' },
            {
                data: 'orderTotal',
                width: '10%',
                render: function (data) {
                    return data != null ? '$' + Number(data).toFixed(2) : '';
                }
            },
            {
                data: 'id',
                orderable: false,
                render: function (data) {
                    return (
                        '<div class="w-75 btn-group" role="group">' +
                        '<a href="/Admin/Order/Details?orderId=' +
                        data +
                        '" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>' +
                        '</div>'
                    );
                },
                width: '10%'
            }
        ]
    });
}
