(function() {
    // 你的默认Token
    const DEFAULT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJQZXJtaXNzaW9uIjpbIkFkYXZuY2VkU2V0dGluZyIsIkRldmljZU1hbmFnZW1lbnQuQWxsIiwiUm9sZU1hbmFnZW1lbnQuQWxsIiwiVXNlck1hbmFnZW1lbnQuQWxsIiwiUG9zaXRpb25Db250cm9sIiwiTm90aWZpY2F0aW9uLkFsbCIsIkFkYXZuY2VkU2V0dGluZyIsIkRldmljZU1hbmFnZW1lbnQuQWxsIiwiUm9sZU1hbmFnZW1lbnQuQWxsIiwiVXNlck1hbmFnZW1lbnQuQWxsIiwiUG9zaXRpb25Db250cm9sIiwiTm90aWZpY2F0aW9uLkFsbCJdLCJnaXZlbl9uYW1lIjoiYWRtaW4iLCJuYmYiOjE3Njk3NTg1NjEsImV4cCI6MTgwMTI5NDU2MSwiaWF0IjoxNzY5NzU4NTYxLCJpc3MiOiJTdXNhbGVtQXBpIiwiYXVkIjoiU3VzYWxlbUFwaVVzZXIifQ.Ubmxwfr18-qnIlReQ1JdXE0qpT2lq7z_R-RUsWaGQg4";

    // 等待Swagger UI渲染完成
    const fillToken = setInterval(function() {
        // 查找Authorize按钮和Modal
        const authorizeBtn = document.querySelector('.authorize.btn');
        const authModal = document.querySelector('.auth-container');
        
        if (authorizeBtn && !document.getElementById('auto-fill-btn')) {
            // 添加"一键填充开发者Token"按钮
            const autoFillBtn = document.createElement('button');
            autoFillBtn.id = 'auto-fill-btn';
            autoFillBtn.className = 'btn authorize';
            autoFillBtn.style.cssText = 'background-color: #49cc90; color: #fff; margin-left: 10px;';
            autoFillBtn.innerText = '填充开发Token (Dev)';
            
            autoFillBtn.addEventListener('click', function() {
                // 点击Authorize按钮打开Modal
                authorizeBtn.click();
                
                // 等待Modal打开
                setTimeout(() => {
                    const tokenInput = document.querySelector('input[name="Bearer"]');
                    const authorizeSubmitBtn = document.querySelector('.auth-btn-wrapper .authorize');
                    
                    if (tokenInput) {
                        tokenInput.value = DEFAULT_TOKEN;
                        // 触发input事件让Swagger识别到值变化
                        tokenInput.dispatchEvent(new Event('input', { bubbles: true }));
                        
                        // 自动点击Authorize提交
                        if (authorizeSubmitBtn) {
                            authorizeSubmitBtn.click();
                            console.log("✅ 开发者Token已自动填充并授权");
                        }
                    }
                }, 300);
            });
            
            // 插入到Authorize按钮后面
            authorizeBtn.parentNode.appendChild(autoFillBtn);
            clearInterval(fillToken);
        }
        
        // 15秒后停止检查，避免内存泄漏
        setTimeout(() => clearInterval(fillToken), 15000);
    }, 500);
})();