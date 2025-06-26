import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Chart, registerables } from 'chart.js';
import ChartDataLables from 'chartjs-plugin-datalabels';
import { CommonModule } from '@angular/common';

Chart.register(...registerables);
Chart.register(ChartDataLables);

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected title = 'angular-test-1';

  http = inject(HttpClient);
  employeeList: any[] = [];
  key = "vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";
  chart: any;

  ngOnInit(): void {
    this.getEmployees();
  }

  getEmployees() {
    this.http.get("https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=" + this.key).subscribe((result:any) => {
      this.employeeList = result;
      this.renderChart();
    })
  }

  getDiffInHours(startDate: string, endDate: string): string {
    const start = new Date(startDate);
    const end = new Date(endDate);
    return ((end.getTime() - start.getTime()) / (1000 * 60 * 60)).toFixed(2);
  }

  sumWorkingHours(employeeName: string): string {
    let sum = 0;
    this.employeeList.forEach(element => {
      if (element.EmployeeName == employeeName) {
        sum += Math.abs(Number(this.getDiffInHours(element.StarTimeUtc, element.EndTimeUtc)));
      }
    });
    return sum.toFixed(2);
  }

  filterEmployeeList(): any[] {
    let filteredList: any[] = [];
    this.employeeList.forEach(element => {
      let flag = 0;
      if (filteredList.length > 0) {
        filteredList.forEach(item => {
          if (item.EmployeeName == element.EmployeeName) {
            flag = 1;
          }
        });
      }
      if (flag == 0) {
        filteredList.push(element);
      }
    });

    for (let i = 0; i < filteredList.length - 1; i++) {
      for (let j = i + 1; j < filteredList.length; j++) {
        if (Number(this.sumWorkingHours(filteredList[i].EmployeeName)) < Number(this.sumWorkingHours(filteredList[j].EmployeeName))) {
          let a = filteredList[i];
          filteredList[i] = filteredList[j];
          filteredList[j] = a;
        }
      }
    }

    return filteredList;
  }

  renderChart() {
    const totals: {[key: string]: number} = {};
    this.filterEmployeeList().forEach(element => {
      totals[element.EmployeeName] = Number(this.sumWorkingHours(element.EmployeeName));
    });

    const labels = Object.keys(totals);
    const data = Object.values(totals).map(val => Number(val));

    this.chart = new Chart("employeePieChart", {
      type: 'pie',
      data: {
        labels: labels,
        datasets: [{
          data: data,
          backgroundColor: [
            '#ff6384', '#36a2eb', '#cc65fe', '#ffce56',
            '#4bc0c0', '#9966ff', '#c9cbcf', '#f67019'
          ],
          borderWidth: 1
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: {
            position: 'right'
          },
          datalabels: {
            formatter: (value, context) => {
              const data = context.chart.data.datasets[0].data as number[];
              const total = data.reduce((sum: number, val: number) => sum + val, 0);
              const percentage = (value / total * 100).toFixed(1) + '%';
              return percentage;
          },
          color: '#fff',
          font: { weight: 'bold', size: 14}
        }}
      },
      plugins: [ChartDataLables]
    });
  }
}