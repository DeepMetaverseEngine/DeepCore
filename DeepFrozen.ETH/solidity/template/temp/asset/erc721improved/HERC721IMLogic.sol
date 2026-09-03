// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./HERC721IMLayout.sol";
import "../ownable/OwnableLogic.sol";
import "../accessControlRef/AccessControlRefLogic.sol";
import "../reentrancy/ReentrancyLogic.sol";
import "../erc721/HERC721Logic.sol";
import "./HERC721IMInterface.sol";

import "../accessControl/AccessControlInterface.sol";

/*
Auto Increasing TokenId and Specified TokenId could be adapted at same time,
The server offline could chose a very big number as offset.
*/
abstract contract HERC721IMLogic is HERC721IMLayout, OwnableLogic, AccessControlRefLogic, ReentrancyLogic, HERC721Logic, HERC721IMInterface {
    using Counters for Counters.Counter;

    using Strings for uint256;
    using SafeMath for uint256;
    using Address for address;

    //have to cooperator with onlyOnce() !!!!!!!!!!!
    modifier enableSudoMint(){
        _sudoMint = true;
        _;
        _sudoMint = false;
    }

    //have to cooperator with onlyOnce() !!!!!!!!!!!
    modifier enableSudoBurn(){
        _sudoBurn = true;
        _;
        _sudoBurn = false;
    }

    //for non-sudo mint, the "to" must be the msg.sender
    function _mintNormal(uint256 tokenId) internal onlyOnce returns (uint256){

        if (tokenId == 0) {
            //starts from 1
            _tokenIdCounter.increment();
            tokenId = _tokenIdCounter.current();
        }

        _mint(msg.sender, tokenId);
        emit Mint(msg.sender, tokenId);

        return tokenId;
    }

    //=========================================

    function _mintSudo(address to, uint256 tokenId) internal enableSudoMint onlyOnce returns (uint256){

        if (tokenId == 0) {
            //starts from 1
            _tokenIdCounter.increment();
            tokenId = _tokenIdCounter.current();
        }

        _mint(to, tokenId);
        emit Mint(to, tokenId);

        return tokenId;
    }

    //=========================================

    //for non-sudo burn, the "from" must be the msg.sender
    function _burnNormal(uint256 tokenId) internal onlyOnce {
        require(_exists(tokenId), "tokenId does not exist");
        require(ownerOf(tokenId) == msg.sender, "can only burn tokenId belongs to yourself");

        _burn(tokenId);

        emit Burn(msg.sender, tokenId);

    }

    //for non-sudo burn, the "from" must be the msg.sender
    function _burnSudo(uint256 tokenId) internal enableSudoBurn onlyOnce {
        require(_exists(tokenId), "tokenId does not exist");

        address owner = ownerOf(tokenId);
        _burn(tokenId);

        emit Burn(owner, tokenId);

    }

    function _setLock(uint256 tokenId, address locker) internal {
        _tokenLocks[tokenId] = locker;
    }

    function _setUint256Attribute(bytes32 attributeName, uint256 tokenId, uint256 attributeValue) internal {
        _fixedAttribute[attributeName][tokenId] = bytes32(attributeValue);
        emit uint256Attribute(attributeName, tokenId, attributeValue);
    }

    function _setBytes32Attribute(bytes32 attributeName, uint256 tokenId, bytes32 attributeValue) internal {
        _fixedAttribute[attributeName][tokenId] = attributeValue;
        emit bytes32Attribute(attributeName, tokenId, attributeValue);
    }

    function _setAddressAttribute(bytes32 attributeName, uint256 tokenId, address attributeValue) internal {
        _fixedAttribute[attributeName][tokenId] = bytes32(uint256(uint160(attributeValue)));
        emit addressAttribute(attributeName, tokenId, attributeValue);
    }

    function _setBytesAttribute(bytes32 attributeName, uint256 tokenId, bytes memory attributeValue) internal {
        _dynamicAttribute[attributeName][tokenId] = attributeValue;
        emit bytesAttribute(attributeName, tokenId, attributeValue);
    }

    //==================================

    function setTokenIdMapRange(uint256 begin, uint256 end) override external onlyOwner {
        require(begin < end, "setTokenIdMapRange, begin < end ?");
        _tokenIdMapRangeBegin = begin;
        _tokenIdMapRangeEnd = end;
    }

    function setTokenIdCounter(uint256 newTokenIdCounter, bool needSub1) override external onlyOwner {
        if (needSub1) {
            newTokenIdCounter = newTokenIdCounter.sub(1);
        }
        _tokenIdCounter._value = newTokenIdCounter;
    }

    function freeze(uint256 tokenId, address unlocker) override virtual external {

        require(_isApprovedOrOwner(_msgSender(), tokenId), "_lock caller is not owner nor approved");
        require(_tokenLocks[tokenId] == address(0), "has been locked");
        _setLock(tokenId, unlocker);

        emit Freeze(unlocker, tokenId);
    }

    function thaw(uint256 tokenId) override virtual external {
        require(_tokenLocks[tokenId] == _msgSender(), "you are not the unlocker, or the token is not locked");
        _setLock(tokenId, address(0));

        emit Thaw(_msgSender(), tokenId);
    }

    function setAccessControl(address accessControl_) override external onlyOwner {
        _setAccessControl(accessControl_);
        emit SetAccessControl(accessControl_);
    }

    function setSupport(bool supportTransfer_, bool supportMint_, bool supportBurn_) override external onlyOwner {
        _supportTransfer = supportTransfer_;
        _supportMint = supportMint_;
        _supportBurn = supportBurn_;
        emit SetSupport(supportTransfer_, supportMint_, supportBurn_);
    }

    function setBlockListFrom(address[] memory from, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < from.length; i++) {
            _blockListFrom[from[i]] = flag;
        }
    }

    function setBlockListTo(address[] memory to, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < to.length; i++) {
            _blockListTo[to[i]] = flag;
        }
    }

    function setPrivilegeListFrom(address[] memory from, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < from.length; i++) {
            _privilegeListFrom[from[i]] = flag;
        }
    }

    function setPrivilegeListTo(address[] memory to, bool flag) override external onlyOwner {
        for (uint256 i = 0; i < to.length; i++) {
            _privilegeListTo[to[i]] = flag;
        }
    }
    //==========

    function tokenIdMapRange() view external returns (uint256, uint256){
        return (_tokenIdMapRangeBegin, _tokenIdMapRangeEnd);
    }


    function frozen(uint256 tokenId) override view external returns (address){
        return _tokenLocks[tokenId];
    }

    function exists(uint256 tokenId) override view external returns (bool) {
        return _exists(tokenId);
    }

    function getUint256Attribute(bytes32 attributeName, uint256 tokenId) override view external returns (uint256) {
        return uint256(_fixedAttribute[attributeName][tokenId]);
    }

    function getBytes32Attribute(bytes32 attributeName, uint256 tokenId) override view external returns (bytes32) {
        return _fixedAttribute[attributeName][tokenId];
    }

    function getAddressAttribute(bytes32 attributeName, uint256 tokenId) override view external returns (address)  {
        return address(uint160(uint256(_fixedAttribute[attributeName][tokenId])));
    }

    function getBytesAttribute(bytes32 attributeName, uint256 tokenId) override view external returns (bytes memory)  {
        return _dynamicAttribute[attributeName][tokenId];
    }

    function support() override view external returns (bool supportTransfer, bool supportMint, bool supportBurn){
        return (_supportTransfer, _supportMint, _supportBurn);
    }

    function transferTxs() override view external returns (uint256) {
        return _transferTxs;
    }

    function interactAmounts() override view external returns (uint256){
        return _interactAmounts;
    }

    function isInteracted(address who) override view external returns (bool){
        return _interactAccount[who];
    }

    function _beforeTokenTransfer(address from, address to, uint256 tokenId) virtual override(HERC721Logic) internal {
        require(tokenId != uint256(0), "tokenId can not be zero");
        require(_tokenLocks[tokenId] == address(0), "can not transfer locked token");

        if (from == address(0) && to != address(0)) {
            //mint mode
            //some how you call _mint :) , you must have done job of mint permission
            _beforeTokenTransferMint(to);

        } else if (from != address(0) && to == address(0)) {
            //burn mode
            //some how you call _burn :) , you must have done job of burn permission
            _beforeTokenTransferBurn(from);
        } else if (from != address(0) && to != address(0)) {
            //transfer
            _beforeTokenTransferTransfer(from, to);
        } else {
            revert("transfer, 'from' and 'to' should never be address(0) at same time");
        }

        //impossible to hit the 2^256 :)
    unchecked{_transferTxs = _transferTxs + 1;}

        if (!_interactAccount[from]) {
            _interactAccount[from] = true;
            _interactAmounts = _interactAmounts.add(1);
        }

        HERC721Logic._beforeTokenTransfer(from, to, tokenId);
    }

    function _beforeTokenTransferMint(address to) view internal {
        AccessControlInterface ac = ac();

        //not support mint
        if (_sudoMint) {
            //pass
        } else {
            if (address(ac) != address(0) && ac.isPrivileged(to)) {
                //privilege pass
            } else if (_privilegeListTo[to]) {
                //privilege pass
            } else if (address(ac) != address(0) && ac.isBlocked(to)) {
                revert("ac.isBlocked-to");
            } else if (_blockListTo[to]) {
                revert("blockListTo");
            } else {
                if (_supportMint) {

                } else {
                    revert("un-support mint");
                }
            }
        }
    }

    function _beforeTokenTransferBurn(address from) view internal {
        AccessControlInterface ac = ac();

        if (_sudoBurn) {
            //pass
        } else {
            if (address(ac) != address(0) && ac.isPrivileged(from)) {
                //privilege overrides blockList
            } else if (_privilegeListFrom[from]) {
                //privilege overrides blockList
            } else if (address(ac) != address(0) && ac.isBlocked(from)) {
                revert("ac.isBlocked-from");
            } else if (_blockListFrom[from]) {
                revert("blockListFrom");
            } else {
                if (_supportBurn) {

                } else {
                    revert("un-support burn");
                }
            }
        }
    }

    function _beforeTokenTransferTransfer(address from, address to) view internal {
        AccessControlInterface ac = ac();

        if (address(ac) != address(0) &&
            (ac.isPrivileged(from) || ac.isPrivileged(to))
        ) {
            //privilege overrides blockList
        } else if (_privilegeListFrom[from] || _privilegeListTo[to]) {
            //privilege overrides blockList
        } else if (address(ac) != address(0) &&
            (ac.isBlocked(from) || ac.isBlocked(to))
        ) {
            revert("ac.isBlocked-from||ac.isBlocked-to");
        } else if (_blockListFrom[from] || _blockListTo[to]) {
            revert("blockListFrom||blockListTo");
        } else {
            if (_supportTransfer) {

            } else {
                revert("un-support transfer");
            }
        }
    }
}
