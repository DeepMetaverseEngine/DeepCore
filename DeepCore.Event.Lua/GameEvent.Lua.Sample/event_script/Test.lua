print('test env',_ENV)
function main(id)

    local helloApi = Api.CreateRemoteApi('hello',id)
    print('before call')
	for k,v in pairs(helloApi.Task) do
		print('helloApi',k,v) 
	end
	Api.Listen.Message(function(ename, msg)
		pprint(ename, msg)
	end)
	Api.Task.Wait()
    print('-----------------------')
end
